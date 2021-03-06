﻿/*HMAC Signature generator example for First Data*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;

using Tricentis.Automation.Engines;
using Tricentis.Automation.Creation;
using Tricentis.Automation.Execution.Results;
using Tricentis.Automation.Engines.SpecialExecutionTasks;
using Tricentis.Automation.AutomationInstructions.TestActions;
using Tricentis.Automation.AutomationInstructions.Dynamic.Values;
using Tricentis.Automation.Engines.SpecialExecutionTasks.Attributes;

namespace SET
{
    [SpecialExecutionTaskName("CalculateHMAC")]
    public class HMACSet : SpecialExecutionTask
    {
        #region Constants

        private const string Key = "Key";
        private const string Secret = "Secret";
        private const string Method = "Method";
        private const string Payload = "Payload";
        private const string TimeStamp = "TimeStamp";
        private const string Signature = "Signature";

        #endregion

        #region Constructor

        public HMACSet(Validator validator) : base(validator)
        {
        }

        #endregion

        #region Public Methods

        public override ActionResult Execute(ISpecialExecutionTaskTestAction testAction)
        {
            String time = String.Empty;
            string hmacSignatureString = String.Empty;

            //TestStep Parameters
            IInputValue key = testAction.GetParameterAsInputValue(Key, false);
            IInputValue secret = testAction.GetParameterAsInputValue(Secret, false);
            IInputValue method = testAction.GetParameterAsInputValue(Method, false);
            IInputValue payload = testAction.GetParameterAsInputValue(Payload, false);
            IInputValue timeStamp = testAction.GetParameterAsInputValue(TimeStamp, true);
            IParameter hmacSignature = testAction.GetParameter("HMAC Signature", 
                                                               false, 
                                                               new[] { ActionMode.Buffer, ActionMode.Verify});
            
            if (key == null || string.IsNullOrEmpty(key.Value))
                throw new ArgumentException(string.Format("Mandatory parameter '{0}' not set.", key));
            if (secret == null || string.IsNullOrEmpty(secret.Value))
                throw new ArgumentException(string.Format("Mandatory parameter '{0}' not set.", secret));
            if (method == null || string.IsNullOrEmpty(method.Value))
                throw new ArgumentException(string.Format("Mandatory parameter '{0}' not set.", method));
            if (payload == null || string.IsNullOrEmpty(payload.Value))
                throw new ArgumentException(string.Format("Mandatory parameter '{0}' not set.", payload));

            //Use timestamp from TestStep parameter otherwise generate one autmatically
            time = (timeStamp == null) ? GetTime().ToString() : timeStamp.Value.ToString();

            //Get HMAC Signature from the provided parameters
            hmacSignatureString = GetHmacSignature(key, secret, method, payload, time);

            if (string.IsNullOrEmpty(hmacSignatureString))
            {
                testAction.SetResultForParameter(hmacSignature,
                                                 SpecialExecutionTaskResultState.Failed,
                                                 "The HMAC Signature was empty or null.");
                return new UnknownFailedActionResult("Could not create HMAC Signature",
                                                     string.Format("Failed while trying to start:\nKey:\r\n {0}\r\nSecret: {1}\r\nMethod: {2}\r\nPayload: {3}\r\nTimeStamp: {4}",
                                                                       key.Value, 
                                                                       secret.Value, 
                                                                       method.Value, 
                                                                       payload.Value, 
                                                                       time),
                                                     "");
            }
            else
            {
                HandleActualValue(testAction, hmacSignature, hmacSignatureString);
                return new PassedActionResult(String.Format("HMAC: {0}\r\n\r\nValues:\r\nKey: {1}\r\nSecret: {2}\r\nMethod: {3}\r\nPayload:\r\n{4}\r\nTimeStamp: {5} ", 
                                                                hmacSignatureString, 
                                                                key.Value, 
                                                                secret.Value, 
                                                                method.Value, 
                                                                payload.Value, 
                                                                time));
            }
        }

        #endregion

        #region Private Methods

        /*Generate the HMAC Signature in accordance to Pre-Request Script provided by First Data*/
        private string GetHmacSignature(IInputValue key, IInputValue secret, IInputValue method, IInputValue payload, string time)
        {

            string rawSignature = key.Value + ':' + time;
            string requestBody = payload.Value;
            var encoding = new System.Text.ASCIIEncoding();
            var sha = new SHA256Managed();
            
            if (method.Value.ToUpper() != "GET" && method.Value.ToUpper() != "DELETE")
            {
                string b64Body = Convert.ToBase64String(sha.ComputeHash(new System.Text.ASCIIEncoding().GetBytes(requestBody)));
                rawSignature = rawSignature + ":" + b64Body;
            }

            byte[] keyByte = encoding.GetBytes(secret.Value);
            byte[] rawSignatureByte = encoding.GetBytes(rawSignature);

            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(rawSignatureByte);
                return Convert.ToBase64String(hashmessage);
            }
        }

        /*Returns Int64 Universal Timestamp*/
        private Int64 GetTime()
        {
            Int64 retval = 0;
            var st = new DateTime(1970, 1, 1);
            TimeSpan t = (DateTime.Now.ToUniversalTime() - st);
            retval = (Int64)(t.TotalMilliseconds + 0.5);
            return retval;
        }

        #endregion
    }
}