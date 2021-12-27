using DDTV_Core.SystemAssembly.BilibiliModule.Rooms;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;

namespace DDTV_WEB_Server.Controllers
{
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class Login : ControllerBase
    {
        [HttpPost(Name = "Login")]
        public string Post([FromForm] string UserName, [FromForm] string Password)
        {
            if (UserName == RuntimeConfig.WebUserName && Password == RuntimeConfig.WebPassword)
            {
                RuntimeConfig.Cookis = Guid.NewGuid().ToString();
                HttpContext.Response.Cookies.Append("DDTVUser", RuntimeConfig.Cookis);
                return MessageBase.Success(nameof(Login), new LoginOK()
                {
                    Cookie= RuntimeConfig.Cookis
                });
            }
            else
            {
                return MessageBase.Success(nameof(Login), "��½��֤ʧ��", "��½��֤ʧ��",MessageBase.code.LoginVeriicationFailed);
            }
        }
        private class LoginOK
        {
            public string Cookie { get; set; }
        }
    }
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class LoginErrer : ControllerBase
    {
        [HttpGet(Name = "LoginErrer")]
        public string get()
        {
            return MessageBase.Success(nameof(LoginErrer), "WEB��½ʧЧ�����µ�½", "WEB��½ʧЧ�����µ�½", MessageBase.code.LoginInfoFailure);
        }
    }
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class AuthenticationFailed : ControllerBase
    {
        [HttpGet(Name = "AuthenticationFailed")]
        public string get(MessageBase.code code,string message)
        {
            return MessageBase.Success(nameof(LoginErrer), message, message, code);
        }
    }
}