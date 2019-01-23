using Habanero.Licensing.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramExtract
{
    public class License
    {
        string _productName;

        string _filePath;
        //create code for applicationsecret
        byte[] _applicationSecret;
        //create code for public key
        byte[] _publicKey;

        public LicenseValidationResult Result { get; private set; }

        public License(string filePath, string applicationSecret, string publicKey, string productName)
        {
            _filePath = filePath;
            _applicationSecret = Convert.FromBase64String(applicationSecret);
            _publicKey = Convert.FromBase64String(publicKey);
            _productName = productName;
        }

        public LicenseValidator Validator
        {
            get
            {
                //this version is for file system - Isolated storage is anther option
                return new LicenseValidator(LicenseLocation.File, _filePath, _productName, _publicKey, _applicationSecret, ThisVersion);

            }
        }

        private static Version ThisVersion
        {
            get
            {
                //Get the executing files filesversion
                var fileversion = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var thisVersion = new Version(fileversion.FileMajorPart, fileversion.FileMinorPart, fileversion.FileBuildPart, fileversion.FilePrivatePart);

                return thisVersion;
            }
        }

        public void DoLicenseCheck()
        {
            LicenseValidationResult result = Validator.CheckLicense();
            Result = result;

            /*if (result.State == LicenseState.Invalid)
            {
                if (result.Issues.Contains(LicenseIssue.NoLicenseInfo))
                {
                    //inform user there is no license info
                }
                else
                {
                    if (result.Issues.Contains(LicenseIssue.ExpiredDateSoft))
                    {
                        //inform user that their license has expired but
                        //that they may continue using the software for a period
                    }
                    if (result.Issues.Contains(LicenseIssue.ExpiredDateHard))
                    {
                        //inform user that their license has expired
                    }
                    if (result.Issues.Contains(LicenseIssue.ExpiredVersion))
                    {
                        //inform user that their license is for an earlier version
                    }
                    //other messages
                }

                //prompt user for trial or to insert license info then decide what to do
                //activate trial
                result = Validator.ActivateTrial(45);
                //or save license
                string userLicense = "Get the license string from your user";
                result = Validator.CheckLicense(userLicense);
                //decide if you want to save the license...
                Validator.SaveLicense(userLicense);
            }
            if (result.State == LicenseState.Trial)
            {
                //activate trial features
            }
            if (result.State == LicenseState.Valid)
            {
                //activate product
                if (Validator.IsEdition("Standard"))
                {
                    //activate pro features...
                }
            }*/
        }
    }
}
