using LlmTornado.Assistants;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

using LlmTornado.Threads;

namespace LlmTornado.Demo;

public class ThreadsDemo : DemoBase
{
    private static TornadoThread? generatedThread;
    private static AssistantMessage? generatedMessage;
    private static TornadoRun? generatedTornadoRun;

    [TornadoTest]
    public static async Task<TornadoThread> CreateThread()
    {
        HttpCallResult<TornadoThread> thread = await Program.Connect().Threads.CreateThread();
        Console.WriteLine(thread.Response);
        generatedThread = thread.Data;
        return thread.Data!;
    }

    [TornadoTest]
    public static async Task<TornadoThread> RetrieveThread()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<TornadoThread> response =
            await Program.Connect().Threads.RetrieveThread(generatedThread.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<TornadoThread> ModifyThread()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<TornadoThread>? response = await Program.Connect().Threads.ModifyThread(generatedThread.Id,
            new ModifyThreadRequest
            {
                Metadata = new Dictionary<string, string>
                {
                    {"key1", "value1"},
                    {"key2", "value2"}
                }
            });
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task DeleteThread()
    {
        generatedThread ??= await CreateThread();
        HttpCallResult<bool> deleted = await Program.Connect().Threads.DeleteThread(generatedThread.Id);
        generatedThread = null;
        generatedMessage = null;
        generatedTornadoRun = null;
        Console.WriteLine(deleted.Response);
    }

    [TornadoTest]
    public static async Task<AssistantMessage> CreateMessage()
    {
        generatedThread ??= await CreateThread();
        AssistantMessage response = await CreateMessage(generatedThread.Id,
            "I need to think of a magic spell that turns cows into sheep.");
        generatedMessage = response;
        return response;
    }

    public static async Task<AssistantMessage> CreateMessage(string threadId, string content)
    {
        HttpCallResult<AssistantMessage> response = await Program.Connect().Threads.CreateMessage(threadId,
            new CreateMessageRequest(content));
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<AssistantMessage> RetrieveMessage()
    {
        generatedThread ??= await CreateThread();
        generatedMessage ??= await CreateMessage();

        HttpCallResult<AssistantMessage> response =
            await Program.Connect().Threads.RetrieveMessage(generatedThread.Id, generatedMessage.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<IReadOnlyList<AssistantMessage>> ListMessages()
    {
        generatedThread ??= await CreateThread();
        generatedMessage ??= await CreateMessage();

        HttpCallResult<ListResponse<AssistantMessage>> response =
            await Program.Connect().Threads.ListMessages(generatedThread.Id);
        Console.WriteLine(response.Response);
        return response.Data!.Items;
    }

    [TornadoTest]
    public static async Task<AssistantMessage> ModifyMessage()
    {
        generatedThread ??= await CreateThread();
        generatedMessage ??= await CreateMessage();

        HttpCallResult<AssistantMessage> response = await Program.Connect().Threads.ModifyMessage(generatedThread.Id,
            generatedMessage.Id, new ModifyMessageRequest
            {
                Metadata = new Dictionary<string, string>
                {
                    {"key1", "value1"},
                    {"key2", "value2"}
                }
            });

        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<bool> DeleteMessage()
    {
        generatedThread = await CreateThread();
        generatedMessage = await CreateMessage();

        HttpCallResult<bool> response =
            await Program.Connect().Threads.DeleteMessage(generatedThread.Id, generatedMessage.Id);
        generatedMessage = null;
        Console.WriteLine(response.Response);
        return response.Data;
    }

    public static async Task<TornadoRun> CreateRun(Assistant? assistant = null, string? assistantInstruction = null)
    {
        generatedMessage ??= await CreateMessage();
        assistant ??= await AssistantsDemo.Create();

        HttpCallResult<TornadoRun> response = await Program.Connect().Threads.CreateRun(generatedThread!.Id,
            new CreateRunRequest(assistant!.Id)
            {
                Model = ChatModel.OpenAi.Gpt4.O241120,
                Instructions = assistantInstruction ??
                               "You are a helpful assistant with the ability to create names of magic spells."
            });
        Console.WriteLine(response.Response);
        generatedTornadoRun = response.Data!;

        return response.Data!;
    }
    
    [TornadoTest]
    public static async Task<TornadoRun> CreateThreadAndRun()
    {
        Assistant? assistant = await AssistantsDemo.Create();
        
        CreateThreadRequest threadRequest = new CreateThreadRequest
        {
            Messages = [ new CreateMessageRequest("I need to think of a magic spell that turns cows into sheep.") ],
            Metadata = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            },
        };

        HttpCallResult<TornadoRun> response = await Program.Connect().Threads.CreateThreadAndRun(
            new CreateThreadAndRunRequest(assistant!.Id, threadRequest)
            {
                Model = ChatModel.OpenAi.Gpt4.O241120,
                Instructions = "You are a helpful assistant with the ability to create names of magic spells."
            });
        Console.WriteLine(response.Response);
        generatedTornadoRun = response.Data!;

        return response.Data!;
    }

    [TornadoTest]
    public static async Task<TornadoRun> RetrieveRun()
    {
        generatedTornadoRun ??= await CreateRun();

        HttpCallResult<TornadoRun> response =
            await Program.Connect().Threads.RetrieveRun(generatedThread!.Id, generatedTornadoRun.Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<TornadoRun> RetrieveRunAndPollForCompletion()
    {
        generatedTornadoRun ??= await CreateRun();

        while (true)
        {
            HttpCallResult<TornadoRun> response = await Program.Connect().Threads
                .RetrieveRun(generatedThread!.Id, generatedTornadoRun.Id);
            if (response.Data!.Status == RunStatus.Completed)
            {
                IReadOnlyList<AssistantMessage> messages = await ListMessages();
                Console.WriteLine(response.Response);

                foreach (AssistantMessage message in messages.Reverse())
                {
                    foreach (MessageContent content in message.Content)
                    {
                        if (content is MessageContentTextResponse text)
                        {
                            Console.WriteLine($"{message.Role}: {text.MessageContentTextData?.Value}");
                        }
                    }
                }

                return response.Data!;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public static async Task<TornadoRun> ModifyRun()
    {
        generatedTornadoRun ??= await CreateRun();

        HttpCallResult<TornadoRun> response = await Program.Connect().Threads.ModifyRun(generatedThread!.Id,
            generatedTornadoRun.Id,
            new ModifyRunRequest
            {
                Metadata = new Dictionary<string, string>
                {
                    {"key1", "value1"},
                    {"key2", "value2"}
                }
            });
        Console.WriteLine(response.Response);
        generatedTornadoRun = response.Data!;
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<IReadOnlyList<TornadoRunStep>> ListRunSteps()
    {
        generatedTornadoRun ??= await RetrieveRunAndPollForCompletion();
        HttpCallResult<ListResponse<TornadoRunStep>> response =
            await Program.Connect().Threads.ListRunSteps(generatedThread!.Id, generatedTornadoRun.Id);
        Console.WriteLine(response.Response);
        return response.Data!.Items;
    }

    [TornadoTest]
    public static async Task<TornadoRunStep> RetrieveRunStep()
    {
        IReadOnlyList<TornadoRunStep> runSteps = await ListRunSteps();
        HttpCallResult<TornadoRunStep> response = await Program.Connect().Threads
            .RetrieveRunStep(generatedThread!.Id, generatedTornadoRun!.Id, runSteps[0].Id);
        Console.WriteLine(response.Response);
        return response.Data!;
    }

    [TornadoTest]
    public static async Task<TornadoRun> ExtractInfoFromFile()
    {
        Assistant assistant = await AssistantsDemo.CreateFileSearchAssistant();
        generatedThread = await CreateThread();
        generatedMessage =
            await CreateMessage(generatedThread.Id, "Please summarize the file in 3 sentences from the file.");
        generatedTornadoRun = await CreateRun(assistant, "You are assistant that analyzes files in a brief way");
        TornadoRun completedRun = await RetrieveRunAndPollForCompletion();
        return completedRun;
    }

    [TornadoTest]
    public static async Task<TornadoRun> RunFunctionAssistant()
    {
        Assistant assistant = await AssistantsDemo.CreateFunctionAssistant();
        generatedThread = await CreateThread();
        generatedMessage =
            await CreateMessage(generatedThread.Id, "What's the weather and humidity in Prague?");
        generatedTornadoRun = await CreateRun(assistant);

        TornadoRun requiredActionRun;

        while (true)
        {
            HttpCallResult<TornadoRun> response = await Program.Connect().Threads
                .RetrieveRun(generatedThread.Id, generatedTornadoRun.Id);

            if (response.Data!.Status is RunStatus.RequiresAction)
            {
                Console.WriteLine(response.Response);
                requiredActionRun = response.Data;
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        // parse the ToolCall to FunctionToolCall
        List<FunctionToolCall> functionCallsWithParameters = requiredActionRun!.RequiredAction!.SubmitToolOutputs.ToolCalls.Where(x => x.Type == ToolCallType.FunctionToolCall).Cast<FunctionToolCall>().ToList();
        List<ToolOutput> toolOutputs = [];
        foreach (FunctionToolCall functionCall in functionCallsWithParameters)
        {
            switch (functionCall.FunctionCall.Name)
            {
                case "get_weather":
                    toolOutputs.Add(new ToolOutput
                    {
                        ToolCallId = functionCall.Id,
                        Output = new Random().Next(-10, 30).ToString()
                    });
                    break;
                case "get_humidity":
                    toolOutputs.Add(new ToolOutput
                    {
                        ToolCallId = functionCall.Id,
                        Output = new Random().Next(0, 100).ToString()
                    });
                    break;
            }
        }

        await Program.Connect().Threads.SubmitToolOutput(generatedThread.Id, generatedTornadoRun.Id, new SubmitToolOutputsRequest(toolOutputs));

        await RetrieveRunAndPollForCompletion();
        return generatedTornadoRun;
    }

    [TornadoTest]
    public static async Task StreamRun()
    {
        generatedMessage = await CreateMessage();
        Assistant? assistant = await AssistantsDemo.Create();

        await Program.Connect().Threads.StreamRun(generatedThread!.Id, new CreateRunRequest(assistant!.Id), new RunStreamEventHandler
        {
            OnMessageDelta = delta =>
            {
                foreach (MessageContent content in delta.Delta.Content)
                {
                    if (content is MessageContentTextResponse text)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(text.MessageContentTextData?.Value);
                        Console.ResetColor();
                    }
                }

                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task CreateThreadAndStreamRun()
    {
        Assistant? assistant = await AssistantsDemo.Create();

        await Program.Connect().Threads.CreateThreadAndStreamRun(new CreateThreadAndRunRequest(assistant!.Id)
        {
            Instructions = "You are a helpful assistant with the ability to create names of magic spells.",
            Thread = new CreateThreadRequest
            {
                Messages = [ new CreateMessageRequest("I need to think of a magic spell that turns cows into sheep.") ],
            }
        }, new RunStreamEventHandler
        {
            OnMessageDelta = delta =>
            {
                foreach (MessageContent content in delta.Delta.Content)
                {
                    if (content is MessageContentTextResponse text)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(text.MessageContentTextData?.Value);
                        Console.ResetColor();
                    }
                }

                return ValueTask.CompletedTask;
            }
        });
    }

    [TornadoTest]
    public static async Task StreamFunctionAssistant()
    {
        Assistant assistant = await AssistantsDemo.CreateFunctionAssistant();
        generatedThread = await CreateThread();
        generatedMessage =
            await CreateMessage(generatedThread.Id, "What's the weather and humidity in Prague? On top of that write also what should we do in this kind of weather and what places are best to visit in this weather. Write at least 2 paragraphs");


        List<ToolOutput> toolOutputs = [];
        string runId = string.Empty;

        await Program.Connect().Threads.StreamRun(generatedThread.Id, new CreateRunRequest(assistant.Id), new RunStreamEventHandler
        {
            OnMessageDelta = delta =>
            {
                foreach (MessageContent content in delta.Delta.Content)
                {
                    if (content is MessageContentTextResponse text)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(text.MessageContentTextData?.Value);
                        Console.ResetColor();
                    }
                }

                return ValueTask.CompletedTask;
            },
            OnRunStatusChanged = (run, status) =>
            {
                if (status is RunStreamEventTypeStatus.RequiresAction)
                {
                    List<FunctionToolCall> functionCallsWithParameters = run.RequiredAction!.SubmitToolOutputs.ToolCalls.Where(x => x.Type == ToolCallType.FunctionToolCall).Cast<FunctionToolCall>().ToList();
                    foreach (FunctionToolCall functionCall in functionCallsWithParameters)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Calling function {functionCall.FunctionCall.Name} with arguments: {functionCall.FunctionCall.Arguments}");
                        Console.ResetColor();
                        switch (functionCall.FunctionCall.Name)
                        {
                            case "get_weather":
                                toolOutputs.Add(new ToolOutput
                                {
                                    ToolCallId = functionCall.Id,
                                    Output = new Random().Next(-10, 30).ToString()
                                });
                                break;
                            case "get_humidity":
                                toolOutputs.Add(new ToolOutput
                                {
                                    ToolCallId = functionCall.Id,
                                    Output = new Random().Next(0, 100).ToString()
                                });
                                break;
                        }
                    }

                    runId = run.Id;
                }

                return ValueTask.CompletedTask;
            },
            OnMessageStatusChanged = (message, status) =>
            {
                if (status is RunStreamEventTypeStatus.Completed)
                {
                    Console.WriteLine("Message completed");
                    foreach (MessageContent content in message.Content)
                    {
                        if (content is MessageContentTextResponse text)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(text.MessageContentTextData?.Value);
                            Console.ResetColor();
                        }
                    }
                }

                return ValueTask.CompletedTask;
            }
        });

        await Program.Connect().Threads.StreamSubmitToolOutput(generatedThread.Id, runId, new SubmitToolOutputsRequest(toolOutputs), new RunStreamEventHandler
        {
            OnMessageDelta = delta =>
            {
                foreach (MessageContent content in delta.Delta.Content)
                {
                    if (content is MessageContentTextResponse text)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(text.MessageContentTextData?.Value);
                        Console.ResetColor();
                    }
                }

                return ValueTask.CompletedTask;
            }
        });
    }

    [TornadoTest]
    public static async Task StreamExtractingInfoFromFile()
    {
        Assistant assistant = await AssistantsDemo.CreateFileSearchAssistant("Static/Files/pride_and_prejudice.txt");
        generatedThread = await CreateThread();
        generatedMessage =
            await CreateMessage(generatedThread.Id, "Create a summary of the story and provide 2 sentences that you like the most and provide source.");
        await Program.Connect().Threads.StreamRun(generatedThread.Id, new CreateRunRequest(assistant.Id), new RunStreamEventHandler
        {
            OnMessageDelta = delta =>
            {
                foreach (MessageContent content in delta.Delta.Content)
                {
                    if (content is MessageContentTextResponse text)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(text.MessageContentTextData?.Value);
                        Console.ResetColor();
                    }
                }

                return ValueTask.CompletedTask;
            },
            OnFinished = () =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            }
        });
    }

    [TornadoTest]
    public static async Task StreamCodeInterpreter()
    {
        Assistant assistant = await AssistantsDemo.CreateWithCodeInterpreter();
        generatedThread = await CreateThread();
        generatedMessage =
            await CreateMessage(generatedThread.Id, "I need to solve the equation `3x + 11 = 14`. Can you help me?");
        await Program.Connect().Threads.StreamRun(generatedThread.Id, new CreateRunRequest(assistant.Id), new RunStreamEventHandler
        {
            OnMessageDelta = delta =>
            {
                foreach (MessageContent content in delta.Delta.Content)
                {
                    if (content is MessageContentTextResponse text)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(text.MessageContentTextData?.Value);
                        Console.ResetColor();
                    }
                }

                return ValueTask.CompletedTask;
            },
            OnRunStepDelta = delta =>
            {
                if (delta.Delta.StepDetails is ToolCallsStepDetails toolCallsStepDetails)
                {
                    foreach (ToolCall toolCall in toolCallsStepDetails.ToolCallItems)
                    {
                        if (toolCall is CodeInterpreterToolCall codeInterpreterToolCall)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write(codeInterpreterToolCall.CodeInterpreter.Input);
                            Console.ResetColor();
                        }
                    }
                }

                return ValueTask.CompletedTask;
            },
            OnMessageStatusChanged = (message, status) =>
            {
                if (status == RunStreamEventTypeStatus.Completed)
                {
                    Console.WriteLine();
                }
                return ValueTask.CompletedTask;
            },
            OnFinished = () =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            }
        });
    }
}