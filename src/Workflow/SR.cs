using System;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.Workflow
{
    internal static class SR
    {
        internal static class Mail
        {
            internal static string Error_UnknownAttachmentType_3 = "$Error_Workflow:Mail_UnknownAttachmentType_3";
        }

        public static string GetString(string fullResourceKey)
        {
            return SenseNetResourceManager.Current.GetString(fullResourceKey);
        }

        public static string GetString(string fullResourceKey, params object[] args)
        {
            return string.Format(GetString(fullResourceKey), args);
        }
    }
}
