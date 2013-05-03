using PD.Base.EntityRepository.ODataClient;
using PD.Base.PortableUtil.Exceptions;
using PD.Base.PortableUtil.Threading;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Scrum.Silverlight
{
	public partial class MainPage : UserControl
	{

		private ScrumClient _scrumClient;

		public MainPage()
		{
			InitializeComponent();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			try
			{
				// TODO: Show a busy animation while initializing
				_scrumClient = new ScrumClient();
				_scrumClient.InitializeTask.ContinueInCurrentSynchronizationContext(task => MessageBox.Show("InitializeTask completed."),// TODO: Turn off busy animation, if there was one.
				                                                                    task => ShowFatalExceptionAndExit("Fatal error initializing web service client", task.Exception));
			}
			catch (Exception ex)
			{
				ShowFatalExceptionAndExit("Fatal error during initialization", ex);
			}
		}

		private void ShowFatalExceptionAndExit(string caption, Exception exception)
		{
			string message = exception.FormatForUser();
			// or: message = exception.ToString();
			MessageBox.Show(/*Application.Current.MainWindow, */message, caption + ", exiting...", MessageBoxButton.OK);
			
			// TODO: Now what to do after a fatal initialization error?
			// This doesn't work in a browser...
			Application.Current.MainWindow.Close();
			// Maybe we should gray the whole windows?
		}

		private void btnLoadProjects_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			try
			{
				// TODO: Do this in an initialization phase - spinning thing etc before the user can do anything
				_scrumClient.EnsureInitializationCompleted();

				var projectsQuery = _scrumClient.Projects.Include(p => p.Areas);
				var completionTask = _scrumClient.InvokeAsync(projectsQuery);

				completionTask.ContinueInCurrentSynchronizationContext(
					(task) =>
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
				MessageBox.Show(ex.FormatForUser());
			}
		}

	}
}
