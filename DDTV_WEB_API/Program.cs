
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography.X509Certificates;
using DDTV_Core.SystemAssembly.ConfigModule;

namespace DDTV_WEB_API//DDTVLiveRecWebServer
{
    public class Program
    {
        private static bool IsSSL = bool.Parse(CoreConfig.GetValue(CoreConfigClass.Key.WEB_API_SSL, "false", CoreConfigClass.Group.WEB_API));
        private static string pfxFileName = CoreConfig.GetValue(CoreConfigClass.Key.pfxFileName, "pfxFileName", CoreConfigClass.Group.WEB_API);
        private static string pfxPasswordFileName = CoreConfig.GetValue(CoreConfigClass.Key.pfxPasswordFileName, "pfxPasswordFileName", CoreConfigClass.Group.WEB_API);
        public static void Main(string[] args)
        {
            {
               
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
                if (IsSSL)
                {
                    builder.WebHost.ConfigureKestrel(options =>
                    {
                        options.ConfigureHttpsDefaults(httpsOptions =>
                        {                         
                            var certPath = Path.Combine(builder.Environment.ContentRootPath, pfxFileName);
                            var keyPath = Path.Combine(builder.Environment.ContentRootPath, pfxPasswordFileName);
                            httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath,keyPath);
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
                app.Urls.Add("http://0.0.0.0:11419");
                app.Urls.Add("https://0.0.0.0:11451");
                app.Run();

            }
            DDTV_Core.InitDDTV_Core.Core_Init(DDTV_Core.InitDDTV_Core.SatrtType.DDTV_WEB);
            BilibiliUserConfig.CheckAccount.CheckAccountChanged += CheckAccount_CheckAccountChanged; ;//ע���½��Ϣ���ʧЧ�¼�
        }

        private static void CheckAccount_CheckAccountChanged(object? sender, EventArgs e)
        {
            Task.Run(() =>
            {
                int i = 0;
                while (i<360)
                {
                    DDTV_Core.SystemAssembly.Log.Log.AddLog("Login", DDTV_Core.SystemAssembly.Log.LogClass.LogType.Error, "�˺ŵ�½ʧЧ��������DDTV���е�½��");
                    Thread.Sleep(10 * 1000);
                    i++;
                }
            });
        }
    }
}