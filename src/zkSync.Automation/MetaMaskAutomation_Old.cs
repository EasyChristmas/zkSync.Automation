using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using zkSync.Automation.Models;

namespace zkSync.Automation
{
    public class MetaMaskAutomation_Old
    {
        /// <summary>
        /// 驱动
        /// </summary>
        private static IWebDriver driver = null;

        /// <summary>
        /// js执行器
        /// </summary>
        private static IJavaScriptExecutor jse = null;

        /// <summary>
        /// 应用配置
        /// </summary>
        public static IConfigurationRoot Configuration;

        /// <summary>
        /// 计时器
        /// </summary>
        private static System.Timers.Timer timer = null;

        /// <summary>
        /// 时间间隔(秒)
        /// </summary>
        private static int interval = 10;

        /// <summary>
        /// 心跳
        /// </summary>
        private static int dida = 0;

        /// <summary>
        /// 重试执行次数
        /// </summary>
        private static int excuteCount = 10;

        /// <summary>
        /// 账号数量
        /// </summary>
        private static int accCount = 0;

        /// <summary>
        /// 转账数量
        /// </summary>
        private static int transCount = 0;

        /// <summary>
        /// 默认网络
        /// </summary>
        private static string defaultNetWork = "Goerli 测试网络";

        /// <summary>
        /// 账户集合
        /// </summary>
        private static List<AccountModel> Accounts = new List<AccountModel>
        {
            new AccountModel{ Address="0x5a2b84d1e33d21dc953bb32d3427e7f207b973ef", Mnemonic="autumn improve fantasy impulse carbon butter chair stock crew elite funny evolve amount surround army reward someone game laundry airport pyramid dance eternal outdoor", PrivateKey="0x0fa42e2c69695c8653b0fdab5102dc438bccdb7b23d8092a1e4016b5d3d64c80" },

            new AccountModel{ Address="0xf16a95ed891fb4be93a8e061e842f4d7c7dc5bc9", Mnemonic="tornado orange slab trap apple purpose feel hurry man clip grass lonely skate style exhaust movie snow update logic boil tortoise find borrow awesome", PrivateKey="0x9af0b2194ff40a6fc778ad34bdc010b8a5fbd59b7a561cbdd9cf804cab3fcbc8" }
        };

        public static void Run()
        {
            Init();

            ZkSyncApps();
        }

        public static void Init()
        {

            //加载配置文件  
            var builder = new ConfigurationBuilder();
            var path = Environment.CurrentDirectory + @"/appsettings.json";
            builder.AddJsonFile(path);
            var config = builder.Build();
            Log("配置文件..");

            var jsonFolder = config["Account:JsonFolder"];

            //校验Json文件
            if (string.IsNullOrWhiteSpace(jsonFolder))
            {
                Log("请先在配置文件中配置Json账号的文件夹地址！");
                return;
            }
            else if (!Directory.Exists(jsonFolder))
            {
                Log($"Json文件夹地址【{jsonFolder}】不存在!");
                return;
            }

            //初始化chrome driver
            if (driver == null)
            {
                InitChromeDriver();
            }
        }

        public static void InitChromeDriver()
        {
            //谷歌浏览器
            var options = new ChromeOptions();
            options.AddArguments(
            "start-maximized"
            //"enable-automation",
            //"--headless",
            //"--no-sandbox", //this is the relevant other arguments came from solving other issues
            //"--disable-infobars",
            //"--disable-dev-shm-usage",
            //"--disable-browser-side-navigation",
            //"--disable-gpu",
            //"--ignore-certificate-errors"
            );

            var path = AppContext.BaseDirectory + "extension_metamask.crx";
            options.AddExtensions(path);
            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

            jse = (IJavaScriptExecutor)driver;
        }

        /// <summary>
        /// 进入ZkSync的Apps页面
        /// </summary>
        public static void ZkSyncApps()
        {
            Log("开始进入zkSync2.0 ~");

            if (driver == null)
            {
                InitChromeDriver();
            }

            driver.Navigate().GoToUrl("https://portal.zksync.io/");

            //等待进入账号界面
            int count = 1;
            while (count <= excuteCount)
            {
                try
                {
                    Log($"等待进入主界面..");
                    Thread.Sleep(1500);
                    var windowHandles = driver.WindowHandles;
                    if (windowHandles.Count >= 1)
                    {
                        InitializeMetaMask();
                        break;
                    }

                }
                catch (Exception ex)
                {
                    Log($"进入主界面失败【{count}】:{ex.Message}");
                    count++;
                }
            }
        }

        /// <summary>
        /// 初始化MetaMask
        /// </summary>
        public static void InitializeMetaMask()
        {
            //切换选项卡
            driver.SwitchTo().Window(driver.WindowHandles[0]);
            Log($"欢迎使用 MetaMask！");

            var metaMaskButton = driver.FindElement(By.XPath("//button[@role='button']"));
            jse.ExecuteScript("arguments[0].click();", metaMaskButton);

            var buttons = driver.FindElements(By.XPath("//button[@role='button']"));
            //导入钱包（默认导入钱包）
            jse.ExecuteScript("arguments[0].click();", buttons[0]);

            Log($"MetaMask 导入/创建钱包！");
            //我同意
            jse.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//button[@data-testid='page-container-footer-next']")));

            var firstAccount = Accounts.FirstOrDefault();
            //导入助记词
            driver.FindElements(By.TagName("input"))[0].SendKeys(firstAccount.Mnemonic);
            driver.FindElement(By.XPath("//input[@id='password']")).SendKeys("wn123456");
            driver.FindElement(By.XPath("//input[@id='confirm-password']")).SendKeys("wn123456");

            var checkbox = driver.FindElements(By.XPath("//div[@role='checkbox']"));
            jse.ExecuteScript("arguments[0].click();", checkbox[1]);

            //完成导入配置
            jse.ExecuteScript("arguments[0].click();", driver.FindElements(By.TagName("button"))[0]);

            //等待导入处理（导入账户可能有点慢）
            int count = 1;
            while (count <= excuteCount)
            {
                try
                {
                    Log($"正在导入账户..");
                    Thread.Sleep(3000);
                    var allCompleted = driver.FindElement(By.XPath($"//button[contains(text(),'全部完成')]"));
                    if (allCompleted != null)
                    {
                        Log($"导入完成！");
                        jse.ExecuteScript("arguments[0].click();", allCompleted);
                        Log($"MetaMask 全部完成！");
                        break;
                    }
                }
                catch
                {
                    count++;
                }
            }

            Thread.Sleep(1000);
            //点击关闭弹框，防止有模态框弹出
            var popover = driver.FindElement(By.XPath("//button[@data-testid='popover-close']"));
            if (popover != null)
            {
                jse.ExecuteScript("arguments[0].click();", popover);
            }

            //设置显示测试网络
            SetTestNetWork();

            //切换到测试网络
            ChangeNetWork();

            //Thread.Sleep(3000);
            //driver.Navigate().GoToUrl("https://portal.zksync.io/");
        }

        /// <summary>
        /// 选择网络
        /// </summary>
        private static void SelectNetWork()
        {
            jse.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//div[contains(@class,'network-display--clickable')]")));
        }

        /// <summary>
        /// 设置显示测试网络
        /// </summary>
        private static void SetTestNetWork()
        {
            SelectNetWork();

            jse.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//a[@class='network-dropdown-content--link']")));


            jse.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath("//div[@data-testid='advanced-setting-show-testnet-conversion'][2]/div[2]/div/div/div[1]")));

            jse.ExecuteScript("arguments[0].click();", driver.FindElement(By.ClassName("settings-page__close-button")));
        }

        /// <summary>
        /// 切换网络
        /// </summary>
        /// <param name="name">网络名称</param>
        private static void ChangeNetWork(string name = null)
        {
            SelectNetWork();
            jse.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath($"//span[contains(text(),'{name ?? defaultNetWork}')]")));
        }

        /// <summary>
        /// 时间事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            dida += interval;
            Log("..");
        }

        /// <summary>
        /// 记录消息
        /// </summary>
        /// <param name="msg"></param>
        public static void Log(string msg)
        {
            Console.WriteLine($"【{ TimeSpan.FromSeconds(dida) }】" + msg);
        }
    }
}
