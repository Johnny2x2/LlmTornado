using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

internal static class ResponseHelpers
{
    /// <summary>
    /// Converts a list of <see cref="ChatMessage"/> objects into a list of <see cref="ResponseInputItem"/> objects.
    /// Maps input and output message properties based on the role and content of each <see cref="ChatMessage"/> in the list.
    /// </summary>
    /// <param name="messages">The list of <see cref="ChatMessage"/> objects representing the conversation history to be converted into response input items.</param>
    /// <returns>A list of <see cref="ResponseInputItem"/> objects created from the provided chat messages, containing either input or output message mappings.</returns>
    public static List<ResponseInputItem> ToResponseInputItems(List<ChatMessage> messages)
    {
        List<ResponseInputItem> items = [];
        
        foreach (ChatMessage chatMessage in messages)
        {
            if (chatMessage.Role is ChatMessageRoles.Assistant)
            {
                OutputMessageInput outputMessage = new OutputMessageInput
                {
                    // leaving default values would trip the API
                    Id = null!,
                    Status = null
                };
                
                if (chatMessage.Content is not null)
                {
                    outputMessage.Content.Add(new ResponseOutputTextContent
                    {
                        Text = chatMessage.Content
                    });
                }
                else if (chatMessage.Refusal is not null)
                {
                    outputMessage.Content.Add(new RefusalContent
                    {
                        Refusal = chatMessage.Refusal
                    });
                }
                
                items.Add(outputMessage);
            }
            else if (chatMessage is {Role: ChatMessageRoles.User, ToolCalls.Count: > 0})
            {
                foreach (ToolCall toolCall in chatMessage.ToolCalls)
                {
                    items.Add(new FunctionToolCallInput(toolCall.Id ?? string.Empty,
                        toolCall.FunctionCall.Name, toolCall.FunctionCall.Arguments));
                }
            }
            else if (chatMessage.FunctionCall?.Result is not null)
            {
                items.Add(new FunctionToolCallOutput(chatMessage.FunctionCall.ToolCall?.Id ?? string.Empty,
                    chatMessage.FunctionCall.Result.Content));
            }
            else
            {
                ResponseInputMessage inputMessage = new ResponseInputMessage();

                if (chatMessage.Content is not null)
                {
                    inputMessage.Content.Add(new ResponseInputContentText(chatMessage.Content));
                }
                
                if (chatMessage.Parts is not null)
                {
                    foreach (ChatMessagePart part in chatMessage.Parts)
                    {
                        switch (part.Type)
                        {
                            case ChatMessageTypes.Text:
                                if (part.Text is not null)
                                {
                                    inputMessage.Content.Add(new ResponseInputContentText(part.Text));
                                }

                                break;
                            case ChatMessageTypes.Image:
                                if (part.Image is not null)
                                {
                                    inputMessage.Content.Add(new ResponseInputContentImage
                                    {
                                        ImageUrl = part.Image.Url,
                                        Detail = part.Image.Detail
                                    });
                                }
                                
                                break;
                            case ChatMessageTypes.FileLink:
                                //TODO: Does the file work across all providers?
                                break;
                        }
                    }
                }
                
                items.Add(inputMessage);
            }
        }
        
        return items;
    }

    /// <summary>
    /// Converts a <see cref="ChatRequest"/> object and an existing <see cref="ResponseRequest"/> object into a new <see cref="ResponseRequest"/> object,
    /// combining and mapping properties such as model, instructions, input items, and other settings.
    /// </summary>
    /// <param name="request">The existing <see cref="ResponseRequest"/> object containing optional property values to be used for the conversion.</param>
    /// <param name="chatRequest">The <see cref="ChatRequest"/> object containing additional or default properties required for the new response request.</param>
    /// <returns>A new <see cref="ResponseRequest"/> object with properties merged and populated from the provided request and chat request objects.</returns>
    public static ResponseRequest ToResponseRequest(IEndpointProvider provider, ResponseRequest? request, ChatRequest chatRequest)
    {
        // warm up the request
        chatRequest.Preserialize(provider);
        request ??= new ResponseRequest();

        string? instructions = request.Instructions;

        if (instructions is null)
        {
            ChatMessage? sysMsg = chatRequest.Messages?.FirstOrDefault(x => x is { Role: ChatMessageRoles.System });

            if (sysMsg is not null)
            {
                if (sysMsg.Content?.Length > 0)
                {
                    instructions = sysMsg.Content;
                }
                else if (sysMsg.Parts?.Count > 0)
                {
                    instructions = string.Join("\n", sysMsg.Parts.Where(x => x.Type is ChatMessageTypes.Text).Select(x => x.Text));
                }
            }
        }
        
        return new ResponseRequest
        {
            Model = request.Model ?? chatRequest.Model,
            Background = request.Background,
            Instructions = instructions,
            InputItems = request.InputItems ?? ToResponseInputItems(chatRequest.Messages ?? []),
            Temperature = request.Temperature ?? chatRequest.Temperature,
            MaxOutputTokens = request.MaxOutputTokens ?? chatRequest.MaxTokens,
            User = request.User ?? chatRequest.User,
            TopP = request.TopP ?? chatRequest.TopP,
            TopLogprobs = request.TopLogprobs ?? chatRequest.TopLogprobs,
            ServiceTier = request.ServiceTier ?? chatRequest.ServiceTier,
            Store = request.Store ?? chatRequest.Store,
            Metadata = request.Metadata ?? chatRequest.Metadata,
            ParallelToolCalls = request.ParallelToolCalls ?? chatRequest.ParallelToolCalls,
            CancellationToken = request.CancellationToken != CancellationToken.None ? request.CancellationToken : chatRequest.CancellationToken,
            Include = request.Include,
            MaxToolCalls = request.MaxToolCalls,
            PreviousResponseId = request.PreviousResponseId,
            Truncation = request.Truncation,
            ToolChoice = request.ToolChoice,
            Tools = request.Tools ?? chatRequest.Tools?.Select(ToResponseTool).OfType<ResponseTool>().ToList(),
            Text = request.Text ?? ToResponseConfiguration(chatRequest.ResponseFormat),
            Prompt = request.Prompt,
            Reasoning = request.Reasoning,
            Stream = false,
        };
    }

    /// <summary>
    /// Converts a <see cref="ChatRequestResponseFormats"/> object into a <see cref="ResponseTextConfiguration"/> object.
    /// Determines the appropriate configuration based on the type of response format specified in the given chat request format.
    /// </summary>
    /// <param name="chatRequestFormat">The <see cref="ChatRequestResponseFormats"/> instance containing information about the response format and associated schema, if applicable.</param>
    /// <returns>A <see cref="ResponseTextConfiguration"/> object representing the configured response based on the provided format, or null if no valid configuration could be determined.</returns>
    public static ResponseTextConfiguration? ToResponseConfiguration(ChatRequestResponseFormats? chatRequestFormat)
    {
        if (chatRequestFormat is not null)
        {
            switch (chatRequestFormat.Type)
            {
                case ChatRequestResponseFormatTypes.Text:
                    return new ResponseTextConfiguration
                    {
                        Format = new ResponseTextFormatConfigurationResponseTextFormat()
                    };
                case ChatRequestResponseFormatTypes.Json:
                    return new ResponseTextConfiguration
                    {
                        Format = new ResponseTextFormatConfigurationJsonObject()
                    };
                case ChatRequestResponseFormatTypes.StructuredJson:
                    if (chatRequestFormat.Schema is not null)
                    {
                        return new ResponseTextConfiguration
                        {
                            Format = new ResponseTextFormatConfigurationJsonSchema
                            {
                                Schema = chatRequestFormat.Schema.Schema,
                                Name = chatRequestFormat.Schema.Name,
                                Strict = chatRequestFormat.Schema.Strict,
                                Description = chatRequestFormat.Schema.Description,
                                Delegate = chatRequestFormat.Schema.Delegate,
                                DelegateMetadata = chatRequestFormat.Schema.DelegateMetadata,
                                Metadata = chatRequestFormat.Schema.ToolMetadata
                            }
                        };
                    }

                    break;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Converts a <see cref="ResponseResult"/> object into a <see cref="ChatResult"/> object,
    /// mapping properties such as ID, model, and output from the response result to the chat result structure.
    /// </summary>
    /// <param name="result">The <see cref="ResponseResult"/> object containing the data to be transformed into a chat result.</param>
    /// <returns>A <see cref="ChatResult"/> object populated with the relevant properties from the provided response result.</returns>
    public static ChatResult ToChatResult(ResponseResult result)
    {
        return new ChatResult
        {
            Id = result.Id,
            Model = result.Model ?? string.Empty,
            Choices = result.Output is not null ? [ ToChatChoice(result) ] : [],
            Usage = result.Usage is not null ? new ChatUsage(result.Usage) : null
        };
    }

    /// <summary>
    /// Converts a <see cref="ResponseResult"/> object into a <see cref="ChatChoice"/> object.
    /// Extracts response details such as message content, reasoning, and parts, and maps them to a structured <see cref="ChatChoice"/> representation.
    /// </summary>
    /// <param name="response">The <see cref="ResponseResult"/> object containing the raw response data to be converted.</param>
    /// <returns>A <see cref="ChatChoice"/> object populated with the structured message and associated components extracted from the input response.</returns>
    public static ChatChoice ToChatChoice(ResponseResult response)
    {
        ChatChoice choice = new ChatChoice();

        choice.Message ??= new ChatMessage();
        choice.Message.Parts ??= [];
        string? textOutput = response.OutputText;
        
        if (textOutput is not null)
        {
            choice.Message.Parts.Add(new ChatMessagePart(textOutput));
            choice.Message.Content = textOutput;
        }
        
        foreach (IResponseOutputItem responseItem in response.Output ?? [])
        {
            if (responseItem.Type == ResponseOutputTypes.Reasoning && responseItem is ResponseReasoningItem reasoningItem)
            {
                string[] reasoning = reasoningItem.Summary.Select(x => x.Text).ToArray();
                choice.Message.ReasoningContent = string.Join("\n", reasoning);
                choice.Message.Parts.Add(new ChatMessagePart(new ChatMessageReasoningData
                {
                    Content = choice.Message.ReasoningContent
                }));
            }
            
            if (responseItem.Type == ResponseOutputTypes.FunctionCall && responseItem is ResponseFunctionToolCallItem functionItem)
            {
                choice.Message.ToolCalls ??= [];
                choice.Message.ToolCallId = functionItem.CallId;
                
                choice.Message.ToolCalls.Add(new ToolCall
                {
                    Id = functionItem.CallId,
                    FunctionCall = new FunctionCall
                    {
                        Arguments = functionItem.Arguments,
                        Name = functionItem.Name,
                        Result = functionItem.Result
                    }
                });
            }
        }
        
        return choice;
    }

    public static Tool? ToChatTool(ResponseTool responseTool)
    {
        if (responseTool.Type is "function" && responseTool is ResponseFunctionTool functionTool)
        {
            return new Tool
            {
                Strict = functionTool.Strict,
                Function = new ToolFunction(functionTool.Name, functionTool.Description ?? string.Empty,
                    functionTool.Parameters)
            };
        }

        return null;
    }
    
    public static ResponseTool? ToResponseTool(Tool tool)
    {
        if (tool.Function?.Parameters != null)
        {
            return new ResponseFunctionTool
            {
                Name = tool.ToolName ?? tool.Function.Name,
                Description = tool.ToolDescription ?? tool.Function.Description,
                Parameters = JObject.FromObject(tool.Function.Parameters),
                Strict = tool.Strict,
                Delegate = tool.Delegate,
                DelegateMetadata = tool.DelegateMetadata,
                Metadata = tool.Metadata
            };
        }

        return null;
    }
}