using TaskSystemClient;
using TaskSystemClient.Models;
using TaskSystemClient.Services;

var config = new RabbitMqConfig();
using var authService = new AuthService();
using var messageService = new UnifiedMessageService(config, authService);
var commandHandler = new JobCommandHandler(messageService, authService);

Console.WriteLine("TaskSystem Client - RabbitMQ Messaging Interface with Authentication");
commandHandler.ShowHelp();

while (true)
{
    Console.Write("Enter command: ");
    var input = Console.ReadLine()?.Trim();

    if (!string.IsNullOrEmpty(input))
    {
        commandHandler.HandleCommand(input);

        if (commandHandler.IsExitCommand)
            break;
    }
}

Console.WriteLine("Application closed.");