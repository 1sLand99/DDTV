
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography.X509Certificates;
using DDTV_Core.SystemAssembly.ConfigModule;

namespace DDTVLiveRecWebServer
{
    public class Program
    {
        private static bool IsLTS = true;
        private static string pfxFileName = CoreConfig.GetValue(CoreConfigClass.Key.DownloadPath, "Rec", CoreConfigClass.Group.WEB_API);
        private static string pfxPasswordFileName = CoreConfig.GetValue(CoreConfigClass.Key.DownloadPath, "Rec", CoreConfigClass.Group.WEB_API);
        public static void Main(string[] args)
        {
            {
                DDTV_Core.InitDDTV_Core.Core_Init(DDTV_Core.InitDDTV_Core.SatrtType.DDTV_WEB);
                WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
                builder.Host.ConfigureServices(Services =>
                {
                    Services.AddControllers();
                    Services.AddEndpointsApiExplorer();
                    Services.AddSwaggerGen();
                    Services.AddMvc();
                    Services.AddControllers();
                    //ע��Cookie��֤����
                    Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, option =>
                        {
                            option.AccessDeniedPath = "/LoginErrer"; //���û����Է�����Դ��û��ͨ���κ���Ȩ����ʱ������������ض�������·����Դ
                            option.LoginPath = "/login/";
                            option.Cookie.Name = "User";//���ô洢�û���¼��Ϣ���û�Token��Ϣ����Cookie����
                            option.Cookie.HttpOnly = true;//���ô洢�û���¼��Ϣ���û�Token��Ϣ����Cookie���޷�ͨ���ͻ���������ű�(��JavaScript��)���ʵ�
                                                          //option.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                                                          //���ô洢�û���¼��Ϣ���û�Token��Ϣ����Cookie��ֻ��ͨ��HTTPSЭ�鴫�ݣ������HTTPЭ�飬Cookie���ᱻ���͡�ע�⣬option.Cookie.SecurePolicy���Ե�Ĭ��ֵ��Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest
                        });

                });
                //builder.Host.ConfigureWebHost(webBuilder =>
                //{
                //    SetAPP(webBuilder);
                //});
                builder.Services.AddSwaggerGen();
                if (IsLTS)
                {
                    builder.WebHost.ConfigureKestrel(options =>
                    {
                        options.ConfigureHttpsDefaults(httpsOptions =>
                        {
                           
                            var certPath = Path.Combine(builder.Environment.ContentRootPath, "./6790481.pem");
                            var keyPath = Path.Combine(builder.Environment.ContentRootPath, "./6790481.key");

                            httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath,
                                                             keyPath);
                        });
                    });
                }
                var app = builder.Build();
                
                //���ڼ���Ƿ�Ϊ��������
                //if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
                
                //app.UseHttpsRedirection();
                app.UseAuthorization();
                app.MapControllers();
                app.UseFileServer(new FileServerOptions()
                {
                    EnableDirectoryBrowsing = false,//�ر�Ŀ¼�ṹ������Ȩ��
                    FileProvider = new PhysicalFileProvider(DDTV_Core.Tool.FileOperation.CreateAll(Environment.CurrentDirectory + @"/static")),
                    RequestPath = new PathString("/static")
                });
                app.Urls.Add("http://0.0.0.0:30086");
                app.Urls.Add("https://0.0.0.0:30087");
                app.Run();

            }
        }
        public static void SetAPP(IWebHostBuilder app)
        {
            if (IsLTS)
            {
                app.ConfigureKestrel(option =>
                {
                    option.ConfigureHttpsDefaults(i =>
                    {
                        i.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2($"./pfx֤������", "pfx֤������");
                    });
                });
                //app.UseStartup<StartupBase>().UseUrls("https://0.0.0.0:10086");
            }
            //else
            //{
            //    app.UseStartup<StartupBase>().UseUrls("http://0.0.0.0:10086");
            //}
        }
        //public static IHostBuilder Start(string[] args) =>
        //    Host.CreateDefaultBuilder(args)
        //        .ConfigureWebHostDefaults(webBuilder =>
        //        {
        //            webBuilder.UseStartup<StartupBase>().UseUrls("http://0.0.0.0:10086");
        //            //webBuilder.UseStartup<Startup>();
        //            SetAPP(webBuilder);
            
        //            webBuilder.Configure(app =>
        //            {
        //                app.UseSwagger();
        //                app.UseSwaggerUI();
        //                app.UseHttpsRedirection();

        //                //�ṩWEB��������Ҫ����Դ�ļ������ļ��е��ļ����������Ȩ
        //                app.UseFileServer(new FileServerOptions()
        //                {
        //                    EnableDirectoryBrowsing = false,//�ر�Ŀ¼�ṹ������Ȩ��
        //                    FileProvider = new PhysicalFileProvider(DDTV_Core.Tool.PathOperation.CreateAll(Environment.CurrentDirectory + @"/static")),
        //                    RequestPath = new PathString("/static")
        //                });
        //            });
                    
        //        });
    }
}