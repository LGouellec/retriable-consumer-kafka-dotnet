using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace external_web_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServiceController : Controller 
    {
        private enum TYPE_ERROR_RAISE
        {
            ERROR_500,
            ERROR_BAD_GATEWAY,
            ERROR_BAD_REQUEST,
            RAISE_TIMEOUT,
            THROW_EXCEPTION
        };
        
        private readonly int percentOfError;
        private readonly Random random;

        public ServiceController(IConfiguration configuration)
        {
            percentOfError = Int32.Parse(configuration["PERCENT_OF_ERROR"] ?? "50");
            random = new Random();
        }
        
        [HttpGet]
        public IActionResult Get()
        {
            bool throwError = ThrowErrorOrNot();
            if (throwError)
            {
                var error = GetNextError();
                switch (error)
                {
                    case TYPE_ERROR_RAISE.ERROR_500:
                        return StatusCode(500);
                    case TYPE_ERROR_RAISE.RAISE_TIMEOUT:
                        Thread.Sleep(3000);
                        return StatusCode(StatusCodes.Status408RequestTimeout);
                    case TYPE_ERROR_RAISE.THROW_EXCEPTION:
                        throw new Exception();
                    case TYPE_ERROR_RAISE.ERROR_BAD_GATEWAY:
                        return StatusCode(StatusCodes.Status502BadGateway);
                    case TYPE_ERROR_RAISE.ERROR_BAD_REQUEST:
                        return StatusCode(StatusCodes.Status400BadRequest);
                }
            }
            
            return Ok();
        }

        private bool ThrowErrorOrNot()
        {
            var next = random.Next(100);
            return next < percentOfError;
        }

        private TYPE_ERROR_RAISE GetNextError()
        {
            string[] names = Enum.GetNames(typeof(TYPE_ERROR_RAISE));
            return Enum.Parse<TYPE_ERROR_RAISE>(names[random.Next(0, names.Length)]);
        }
    }
}