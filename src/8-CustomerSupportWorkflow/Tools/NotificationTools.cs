using System.ComponentModel;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace _8_CustomerSupportFanInWorkflow.Tools;

internal static class NotificationTools
{
    [Description("Send an SMS to the CEO when the ticket is urgent. Use this to notify the CEO about critical escalations.")]
    public static string SendSms(string body)
    {
        var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
            ?? throw new InvalidOperationException("TWILIO_ACCOUNT_SID environment variable is not set.");
        var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
            ?? throw new InvalidOperationException("TWILIO_AUTH_TOKEN environment variable is not set.");

        TwilioClient.Init(accountSid, authToken);

        var messageOptions = new CreateMessageOptions(new PhoneNumber("+359888778877"))
        {
            From = new PhoneNumber("+16416145641"),
            Body = body
        };

        var message = MessageResource.Create(messageOptions);

        return message.Body;
    }
}
