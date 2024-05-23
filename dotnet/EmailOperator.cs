namespace EmailOperator {
    public class EmailOperator {
        public static async Task Main(string[] args){
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) => {
                services.AddSingleton<IKubernetes>(sp => {
                    var config = KubernetesClientConfiguration.BuildDefaultConfig();
                    return new Kubernetes(config);
                });

                services.AddTransient<EmailController>();
                services.AddTransient<EmailSenderConfigController>();
                services.AddHostedService<OperatorHostedService>();
            });
   
        public class OperatorHostedService : BackgroundService {
            private readonly IKubernetes _kubernetes;
            private readonly ILogger<OperatorHostedService> _logger;
            private readonly EmailController _emailController;
            private readonly EmailSenderConfigController _emailSenderConfigController;

            public OperatorHostedService(IKubernetes kubernetes, ILogger<OperatorHostedService> logger, EmailController emailController, EmailSenderConfigController emailSenderConfigController) {
                _kubernetes = kubernetes;
                _logger = logger;
                _emailController = emailController;
                _emailSenderConfigController = emailSenderConfigController;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
                var emailController = new Operator<Email, EmailSpec, EmailStatus>(_kubernetes, _logger);
                emailController.OnUpdate += async email => await _emailController.ReconcileAsync(email);
                
                var emailSenderConfigController = new Operator<EmailSenderConfig, EmailSenderConfigSpec>(_kubernetes, _logger);
                emailSenderConfigController.OnUpdate += async emailSenderConfig => await _emailSenderConfigController.ReconcileAsync(emailSenderConfig);

                await Task.WhenAll(
                    emailController.StartAsync(stoppingToken);
                    emailSenderConfigController.StartAsync(stoppingToken);
                );
            }
        }
    } 
}