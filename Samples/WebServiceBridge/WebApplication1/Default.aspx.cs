using System;
using System.Web.UI;
using WebApplication1.localhost;

namespace WebApplication1
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            int number = int.Parse(TextBox1.Text);
            var command = new Command {Id = number};

            var service = new Service1();
            var result = service.Process(command);

            Label1.Text = Enum.GetName(typeof (ErrorCodes), result);
        }
    }
}
