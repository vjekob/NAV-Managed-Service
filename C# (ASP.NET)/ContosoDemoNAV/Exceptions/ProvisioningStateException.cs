using System;

namespace ContosoDemoNAV.Exceptions
{
    public class ProvisioningStateException : Exception
    {
        public ProvisioningStateException(string message) : base(message)
        {
        }
    }
}