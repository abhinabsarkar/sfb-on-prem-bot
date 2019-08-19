using SkypeConsoleBot.Services.Helper;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations.Helper
{
    class TextExtraction : ITextExtraction
    {
        private readonly string _hrefString;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public TextExtraction(string hrefString)
        {
            _hrefString = hrefString;
        }

        // This function converts HTML code to plain text
        // Any step is commented to explain it better
        // You can change or remove unnecessary parts to suite your needs
        public async Task<string> GetMessageFromHref(string hrefString)
        {
            string message = hrefString.Substring(hrefString.IndexOf(',') + 1);
            message = message.Replace('+', ' ');
            if (message.IndexOf("%0d") > -1)
                return message.Remove(message.IndexOf("%0d"));
            else
                return message;
        }

        public async Task<string> GetMessageFromHtml(string hrefString)
        {
            string message;
            //Code to handle the additional charaters added in skype when a space is added in the end or if the text is pasted on Skype
            if (hrefString.Contains("%3c%2fspan%3e%3cspan"))
            {
                message = hrefString.Substring(hrefString.IndexOf("%3b%22%3e", hrefString.IndexOf("%3b%22%3e") + 1)).Replace("%3b%22%3e", "");// ("data:text/html;charset=utf-8,%3cspan+style%3d%22margin-bottom%3a0pt%3bline-height%3anormal%3b%22%3e%3cspan+style%3d%22font-size%3a10pt%3bfont-family%3a%26quot%3bSegoe+UI%26quot%3b%2csans-serif%3bcolor%3ablack%3b%22%3ehow+to+build+an+API+%3f%3c%2fspan%3e%3cspan+style%3d%22font-size%3a10pt%3bfont-family%3a%26quot%3bSegoe+UI%26quot%3b%2csans-serif%3bcolor%3ablack%3b%22%3e%26%23160%3b%3c%2fspan%3e%3c%2fspan%3e", "");// (hrefString.IndexOf(',', hrefString.IndexOf(',') + 1);
                message = message.Substring(0, message.IndexOf("%3c%2fspan%3e"));
            }
            else
            {
                message = hrefString.Substring(hrefString.LastIndexOf("%3b%22%3e")).Replace("%3b%22%3e", "");// ("data:text/html;charset=utf-8,%3cspan+style%3d%22font-size%3a10pt%3bfont-family%3aSegoe+UI%3bcolor%3a%23000000%3b%22%3e", "");// (hrefString.IndexOf(',') + 1);
                message = message.Replace("%3c%2fspan%3e", "");
            }
            message = message.Replace("+", " ");

            //Added code to handle additional characters added by manually adding space in the end
            message = message.Replace("%26%23160%3b", "");

            return message;
        }
    }
}
