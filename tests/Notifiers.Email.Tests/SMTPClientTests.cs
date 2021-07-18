using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Xunit;

namespace Notifiers.Email.Tests
{
    public class SMTPClientTests
    {
        public class Send : IDisposable
        {
            private readonly SmtpClient _client;
            private readonly string _user;
            private readonly MailMessage _message;
            private Exception error;

            public Send()
            {
                _user = Environment.GetEnvironmentVariable("email_username", EnvironmentVariableTarget.User);

                _client = new SmtpClient();
                _client.SendCompleted += ClientOnSendCompleted;
                _message = new MailMessage(_user, _user, "test", "Hi!");;
            }

            private void ClientOnSendCompleted(object sender, AsyncCompletedEventArgs e)
            {
                error = e.Error;
            }

            [Fact]
            public void WhenGmail_EmailSentWithNoError()
            {
                UseGmail();

                _client.Send(_message);

                error.Should().BeNull();
            }

            [Fact]
            public void WhenLocalhostDirectory_EmailSentWithNoError()
            {
                UseLocalhostDirectory();

                _client.Send(_message);

                error.Should().BeNull();
            }

            [Fact]
            public void AzureSSLNetwork_EmailSentWithNoError()
            {
                UseAzureSSLNetwork();

                _client.Send(_message);

                error.Should().BeNull();
            }

            public void Dispose()
            {
                _client.Dispose();
            }

            private void UseGmail()
            {
                _client.Host = "smtp.gmail.com";
                _client.Port = 587;
                _client.DeliveryMethod = SmtpDeliveryMethod.Network;
                // UseDefaultCredentials resets credentials. Make sure this is before setting credentials.
                _client.UseDefaultCredentials = false;
                _client.EnableSsl = true;

                var password = Environment.GetEnvironmentVariable("email_password", EnvironmentVariableTarget.User);
                // Gmail credentials of that user
                _client.Credentials = new NetworkCredential(_user, password);
            }

            private void UseLocalhostDirectory()
            {
                _client.Host = "localhost";
                _client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                // Make sure the folder exists
                _client.PickupDirectoryLocation = @"C:\emails";
            }

            private void UseAzureSSLNetwork()
            {
                // Connecting through IP address does not work.
                // You will need to add an entry in the hosts file to recognize Windows-Server (IP DNS)
                _client.Host = "Windows-Server";
                _client.DeliveryMethod = SmtpDeliveryMethod.Network;
                _client.Port = 25;
                _client.EnableSsl = true;
                // Make sure cert with PK is installed on SMTP server (any TLS cert)
                // Make sure cert with FK is installed on this machine
                var cert = FindCertificate("4197D86EF230F5E475C8458C60523ADD344BB78D");
                _client.ClientCertificates.Add(cert);
                _client.UseDefaultCredentials = true;
            }

            private X509Certificate2 FindCertificate(string thumbprint)
            {
                var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                var cert = store.Certificates
                    .OfType<X509Certificate2>()
                    .FirstOrDefault(x => x.Thumbprint == thumbprint);
                store.Close();

                return cert;
            }
        }
    }
}
