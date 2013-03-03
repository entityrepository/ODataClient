using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PD.Base.EntityRepository.ODataClient;
using PD.Base.PortableUtil.Threading;
using Scrum.Model;

namespace Scrum.Silverlight
{
	public partial class MainPage : UserControl
	{

		private ScrumClient _scrumClient = new ScrumClient();

		public MainPage()
		{
			InitializeComponent();
		}

		private void btnLoadProjects_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				// TODO: Do this in an initialization phase - spinning thing etc before the user can do anything
				_scrumClient.EnsureInitializationCompleted();

				var projectsQuery = _scrumClient.Projects.Include(p => p.Areas);
				var completionTask = _scrumClient.InvokeAsync(projectsQuery);

				completionTask.ContinueWithCurrentSynchronizationContext((task) =>
				                                                         {
					                                                         if (task.IsFaulted)
					                                                         {
						                                                         MessageBox.Show(task.Exception.ToString());
						                                                         return;
					                                                         }
					                                                         cboProjects.ItemsSource = projectsQuery;
				                                                         });
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}
	}
}
