﻿using CLI.WebAppServices.Middleware;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using static CLI.WebAppServices.Middleware.InterfaceAuthentication;

namespace CLI.WebAppServices.Api
{
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [Route("api/login/[controller]")]
    public class get_login_qr : ControllerBase
    {
        /// <summary>
        /// 获取登陆二维码
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "get_login_qr")]
        public async Task<ActionResult> Get()
        {
            int waitTime = 0;
            while (waitTime <= 3000)
            {
                if (System.IO.File.Exists(Core.Config.Core._QrFileNmae))
                {
                    FileInfo fi = new FileInfo(Core.Config.Core._QrFileNmae);
                    using (FileStream fs = fi.OpenRead())
                    {
                        byte[] buffer = new byte[fi.Length];
                        //读取图片字节流
                        await fs.ReadAsync(buffer, 0, Convert.ToInt32(fi.Length));
                        return File(buffer, "image/png");
                    }
                }
                else
                {
                    await Task.Delay(1000);
                    waitTime += 1000;
                }
            }
            return Content(MessageBase.Success(nameof(use_agree), false, $"登陆二维码不存在，请检查是否调用登陆接口且未过期", MessageBase.code.OperationFailed), "application/json");
        }
    }
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [Route("api/login/[controller]")]
    [Login]
    public class use_agree : ControllerBase
    {
        /// <summary>
        /// 同意用户协议
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        [HttpPost(Name = "use_agree")]
        public ActionResult Post([FromForm] string check = "n")
        {
            if (check == "y")
            {
                Core.Config.Core._UseAgree = true;
                return Content(MessageBase.Success(nameof(use_agree), true, $"用户已同意使用须知"), "application/json");
            }
            else
            {
                Core.Config.Core._UseAgree = false;
                return Content(MessageBase.Success(nameof(use_agree), false, $"用户未同意使用须知", MessageBase.code.LoginInfoFailure), "application/json");
            }
        }
    }
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [Route("api/login/[controller]")]
    [Login]
    public class re_login : ControllerBase
    {
        /// <summary>
        /// 重新登陆
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "re_login")]
        public async Task<ActionResult> Post(PostCommonParameters commonParameters)
        {
            await Login.QR();
            return Content(MessageBase.Success(nameof(re_login), true, $"触发登陆功能，请在1分钟内使用get_login_qr获取登陆二维码进行登陆", MessageBase.code.LoginInfoFailure), "application/json");
        }
    }
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [Route("api/login/[controller]")]
    [Login]
    public class use_agree_state : ControllerBase
    {
        /// <summary>
        /// 获得用户初始化授权状态
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "use_agree_state")]
        public ActionResult Post(PostCommonParameters commonParameters)
        {
            return Content(MessageBase.Success(nameof(use_agree_state), Core.Config.Core._UseAgree, $"获取用户初始化授权状态"), "application/json");
        }
    }
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [Route("api/login/[controller]")]
    [Login]
    public class get_login_status : ControllerBase
    {
        /// <summary>
        /// 获取本地登录态AccountInformation的有效状态
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "get_login_status")]
        public ActionResult Post(PostCommonParameters commonParameters)
        {
            return Content(MessageBase.Success(nameof(get_login_status), Core.RuntimeObject.Account.GetLoginStatus(), $"获取本地登录态AccountInformation的有效状态"), "application/json");
        }
    }
}
