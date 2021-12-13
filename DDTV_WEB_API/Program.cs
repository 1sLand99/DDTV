using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;

namespace DDTVLiveRecWebServer
{
    public class Program
    {
        private static bool IsLTS = false;
        public static void Main(string[] args)
        {
            #region Init
            {
            }
            #endregion
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
                            option.Cookie.Name = "DDTVUser";//���ô洢�û���¼��Ϣ���û�Token��Ϣ����Cookie����
                            option.Cookie.HttpOnly = true;//���ô洢�û���¼��Ϣ���û�Token��Ϣ����Cookie���޷�ͨ���ͻ���������ű�(��JavaScript��)���ʵ�
                                                          //option.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                                                          //���ô洢�û���¼��Ϣ���û�Token��Ϣ����Cookie��ֻ��ͨ��HTTPSЭ�鴫�ݣ������HTTPЭ�飬Cookie���ᱻ���͡�ע�⣬option.Cookie.SecurePolicy���Ե�Ĭ��ֵ��Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest
                        });

                });
                //builder.Host.ConfigureWebHost(webBuilder => {
                //    webBuilder.UseStartup<StartupBase>().UseUrls("http://0.0.0.0:10086");
                //    //webBuilder.UseStartup<Startup>();
                //    SetAPP(webBuilder);

                //    webBuilder.Configure(app =>
                //    {
                //        app.UseSwagger();
                //        app.UseSwaggerUI();
                //        app.UseHttpsRedirection();

                //        //�ṩWEB��������Ҫ����Դ�ļ������ļ��е��ļ����������Ȩ
                //        app.UseFileServer(new FileServerOptions()
                //        {
                //            EnableDirectoryBrowsing = false,//�ر�Ŀ¼�ṹ������Ȩ��
                //            FileProvider = new PhysicalFileProvider(DDTV_Core.Tool.PathOperation.CreateAll(Environment.CurrentDirectory + @"/static")),
                //            RequestPath = new PathString("/static")
                //        });
                //    });

                //});
                
                var app = builder.Build();
                
                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
                app.UseHttpsRedirection();
                app.UseAuthorization();
                app.MapControllers();
                app.UseFileServer(new FileServerOptions()
                {
                    EnableDirectoryBrowsing = false,//�ر�Ŀ¼�ṹ������Ȩ��
                    FileProvider = new PhysicalFileProvider(DDTV_Core.Tool.PathOperation.CreateAll(Environment.CurrentDirectory + @"/static")),
                    RequestPath = new PathString("/static")
                });
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
                app.UseStartup<StartupBase>().UseUrls("https://0.0.0.0:10086");
            }
            else
            {
                app.UseStartup<StartupBase>().UseUrls("http://0.0.0.0:10086");
            }
        }
       
    }
}