using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSLinkHelperApp
{
    public class ErrorMessagesProvider
{
    public static readonly Dictionary<string, string> ErrorMessages = new Dictionary<string,string>()
    {
        { "UnknownError", "Unknown Error" },
        { "Ok", "OK. No error happens" },
        { "RecvAckTimeout", "ECR receive ACK timeout" },
        { "RecvDataTimeout", "ECR receive data timeout" },
        { "ConnectError", "ECR can't connect to a terminal" },
        { "SendDataError", "ECR send data error" },
        { "RecvAckError", "ECR receive ACK error" },
        { "RecvDataError", "ECR receive data Error" },
        { "ExceptionalHttpStatusCode", "Http Status Code isn't 200" },
        { "LrcError", "Check response LRC error" },
        { "PackRequestError", "Some parameters error." },
        { "RequestDataIsNull", "Command request is null." }
    };

    public static string GetErrorMessage(string errorCode)
    {
         string message;
         if (ErrorMessages.TryGetValue(errorCode, out message))
             {
               return message;
              }   
          return "Unknown Error Code";
    }
}

}
