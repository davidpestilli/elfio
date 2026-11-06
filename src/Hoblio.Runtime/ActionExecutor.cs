using System.Net.Http;
using System.Text;
using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Runtime;

public static class ActionExecutor
{
    static readonly HttpClient Http = new();

    public static string Execute(
        ActionKind action,
        BrowserContext browser,
        ExcelContext excel,
        WordContext word,
        PdfContext pdf,
        RunVariables vars,
        string? projectRoot,
        CancellationToken cancellationToken = default)
    {
        if (action is RunFlowAction)
            throw new InvalidOperationException("run_flow is executed by the graph runner, not execute_action");

        return action switch
        {
            LogAction log => $"log: {RunVariables.Interpolate(log.Message, vars)}",
            NoOpAction => "noop",
            FileWriteAction fw => FileActions.FileWrite(
                RunVariables.Interpolate(fw.Path, vars),
                RunVariables.Interpolate(fw.Content, vars),
                projectRoot),
            FileAppendAction fa => FileActions.FileAppend(
                RunVariables.Interpolate(fa.Path, vars),
                RunVariables.Interpolate(fa.Content, vars),
                projectRoot),
            FileReadAction fr => FileActions.FileReadIntoVariable(fr.Path, fr.Variable, vars, projectRoot),
            ListAppendAction la => ListActions.ListAppend(vars, la.ListVariable, la.Item),
            ListClearAction lc => ListActions.ListClear(vars, lc.ListVariable),
            DelayAction d => Delay(d.Milliseconds),
            HttpGetAction h => HttpGet(h, vars, cancellationToken),
            HttpRequestAction r => HttpRequest(r, vars, cancellationToken),
            LlmCompletionAction l => LlmCompletion(l, vars, cancellationToken),
            WebAutomationAction w => browser.Execute(w, vars, projectRoot),
            ShowMessageAction sm => ShowMessage(sm, vars),
            ShowGetUserInputAction ui => ShowGetUserInput(ui, vars),
            ShowSelectFromListAction sl => ShowSelectFromList(sl, vars),
            SetVariableAction sv => SetVariable(sv, vars),
            IncreaseVariableAction or DecreaseVariableAction => AdjustVariable.ExecuteAdjust(action, vars, 1.0),
            ExcelAction ex => excel.Execute(ex, vars, projectRoot),
            WordAction w => word.Execute(w, vars, projectRoot),
            PdfAction p => pdf.Execute(p, vars, projectRoot),
            VariablesAction va => VariablesContext.Execute(va, vars, projectRoot),
            _ => throw new InvalidOperationException($"unknown action {action.Type}"),
        };
    }

    public static Dictionary<string, string>? SnapshotVariables(ActionKind action, RunVariables vars)
    {
        var outMap = new Dictionary<string, string>();
        foreach (var name in action.ProducedVariables())
        {
            if (vars.TryGetValue(name, out var value))
                outMap[name] = value;
        }
        return outMap.Count == 0 ? null : outMap;
    }

    static string Delay(ulong ms)
    {
        var remaining = (int)Math.Min(ms, int.MaxValue);
        while (remaining > 0)
        {
            if (RunCancel.IsRequested)
                throw new OperationCanceledException("run cancelled by user");
            var step = Math.Min(remaining, 100);
            Thread.Sleep(step);
            remaining -= step;
        }
        return $"delay: {ms}ms";
    }

    static string HttpGet(HttpGetAction action, RunVariables vars, CancellationToken cancellationToken)
    {
        var url = RunVariables.Interpolate(action.Url, vars);
        using var response = Http.GetAsync(url, cancellationToken).GetAwaiter().GetResult();
        var body = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(action.Variable))
            vars[action.Variable.Trim()] = body;
        var preview = body.Length > 120 ? body[..120] : body;
        var ellipsis = body.Length > 120 ? "…" : "";
        var storeNote = string.IsNullOrWhiteSpace(action.Variable) ? "" : $" → {action.Variable.Trim()}";
        return $"http_get: {url} -> {(int)response.StatusCode} ({body.Length} bytes){storeNote}: {preview}{ellipsis}";
    }

    static string HttpRequest(HttpRequestAction action, RunVariables vars, CancellationToken cancellationToken)
    {
        var url = RunVariables.Interpolate(action.Url, vars);
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("http_request: url is empty");

        using var request = new HttpRequestMessage(ResolveHttpMethod(action.Method), url);

        string? contentTypeFromHeaders = null;
        foreach (var (name, value) in ParseCustomHeaders(action.CustomHeaders, vars))
        {
            if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                contentTypeFromHeaders = value;
                continue;
            }
            request.Headers.TryAddWithoutValidation(name, value);
        }

        if (!string.IsNullOrWhiteSpace(action.Accept))
            request.Headers.TryAddWithoutValidation("Accept", RunVariables.Interpolate(action.Accept, vars));

        var bodyRaw = action.Body is null ? null : RunVariables.Interpolate(action.Body, vars);
        if (!string.IsNullOrEmpty(bodyRaw))
        {
            var ct = !string.IsNullOrWhiteSpace(action.ContentType)
                ? RunVariables.Interpolate(action.ContentType!, vars)
                : contentTypeFromHeaders ?? "application/json; charset=utf-8";
            request.Content = new StringContent(bodyRaw, Encoding.UTF8, ParseMediaType(ct));
        }

        using var response = Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).GetAwaiter().GetResult();
        var responseBody = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        var code = (int)response.StatusCode;

        if (!string.IsNullOrWhiteSpace(action.ResponseBodyVariable))
            vars[action.ResponseBodyVariable!.Trim()] = responseBody;
        if (!string.IsNullOrWhiteSpace(action.ResponseHeadersVariable))
            vars[action.ResponseHeadersVariable!.Trim()] = FormatResponseHeaders(response);
        if (!string.IsNullOrWhiteSpace(action.StatusCodeVariable))
            vars[action.StatusCodeVariable!.Trim()] = code.ToString();

        var preview = responseBody.Length > 120 ? responseBody[..120] : responseBody;
        var ellipsis = responseBody.Length > 120 ? "…" : "";
        var outs = new List<string>();
        if (!string.IsNullOrWhiteSpace(action.ResponseBodyVariable))
            outs.Add($"body→{action.ResponseBodyVariable.Trim()}");
        if (!string.IsNullOrWhiteSpace(action.ResponseHeadersVariable))
            outs.Add($"headers→{action.ResponseHeadersVariable.Trim()}");
        if (!string.IsNullOrWhiteSpace(action.StatusCodeVariable))
            outs.Add($"status→{action.StatusCodeVariable.Trim()}");
        var outNote = outs.Count == 0 ? "" : " " + string.Join(", ", outs);

        return $"http_request {request.Method.Method} {url} -> {code} ({responseBody.Length} bytes){outNote}: {preview}{ellipsis}";
    }

    static string LlmCompletion(LlmCompletionAction action, RunVariables vars, CancellationToken cancellationToken)
    {
        var url = RunVariables.Interpolate(action.Url, vars);
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("llm_completion: url is empty");

        var model = RunVariables.Interpolate(action.Model, vars);
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("llm_completion: model is empty");

        var outVar = action.ResponseBodyVariable?.Trim();
        if (string.IsNullOrWhiteSpace(outVar))
            throw new InvalidOperationException("llm_completion: response_body_variable is required");

        var style = (action.ApiStyle ?? "ollama_chat").Trim().ToLowerInvariant();
        if (style.Length == 0)
            style = "ollama_chat";

        if (!LlmHasAnyPromptContent(action, vars))
            throw new InvalidOperationException("llm_completion: set at least one of system, user, or document");

        var jsonBody = style switch
        {
            "openai_chat" or "openai" or "openai_chat_completions" => BuildOpenAiChatCompletionBody(action, vars, model),
            "ollama_chat" or "ollama" => BuildOllamaChatBody(action, vars, model),
            _ => throw new InvalidOperationException(
                $"llm_completion: unknown api_style `{action.ApiStyle}` (use ollama_chat or openai_chat)"),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var response = Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).GetAwaiter().GetResult();
        var responseBody = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        vars[outVar] = responseBody;

        var code = (int)response.StatusCode;
        var preview = responseBody.Length > 80 ? responseBody[..80] + "…" : responseBody;
        return $"llm_completion: {style} POST {url} -> {code} ({responseBody.Length} chars) → {outVar}: {preview}";
    }

    static string BuildOllamaChatBody(LlmCompletionAction action, RunVariables vars, string model)
    {
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteString("model", model);
            w.WritePropertyName("messages");
            w.WriteStartArray();
            if (!string.IsNullOrWhiteSpace(action.System))
            {
                w.WriteStartObject();
                w.WriteString("role", "system");
                w.WriteString("content", RunVariables.Interpolate(action.System.Trim(), vars));
                w.WriteEndObject();
            }

            w.WriteStartObject();
            w.WriteString("role", "user");
            w.WriteString("content", LlmCombineUserContent(action, vars));
            w.WriteEndObject();
            w.WriteEndArray();

            w.WriteBoolean("stream", false);

            WriteLlmOptionalTemperature(w, action, vars);

            if (!string.IsNullOrWhiteSpace(action.JsonSchema))
            {
                var js = RunVariables.Interpolate(action.JsonSchema, vars).Trim();
                w.WritePropertyName("format");
                if (string.Equals(js, "json", StringComparison.OrdinalIgnoreCase))
                    w.WriteStringValue("json");
                else
                {
                    using var doc = JsonDocument.Parse(js);
                    w.WriteRawValue(doc.RootElement.GetRawText());
                }
            }

            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    static string BuildOpenAiChatCompletionBody(LlmCompletionAction action, RunVariables vars, string model)
    {
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteString("model", model);
            w.WritePropertyName("messages");
            w.WriteStartArray();
            if (!string.IsNullOrWhiteSpace(action.System))
            {
                w.WriteStartObject();
                w.WriteString("role", "system");
                w.WriteString("content", RunVariables.Interpolate(action.System.Trim(), vars));
                w.WriteEndObject();
            }

            w.WriteStartObject();
            w.WriteString("role", "user");
            w.WriteString("content", LlmCombineUserContent(action, vars));
            w.WriteEndObject();
            w.WriteEndArray();

            WriteLlmOptionalTemperature(w, action, vars);

            if (!string.IsNullOrWhiteSpace(action.JsonSchema))
            {
                var js = RunVariables.Interpolate(action.JsonSchema, vars).Trim();
                if (string.Equals(js, "json_object", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(js, "json", StringComparison.OrdinalIgnoreCase))
                {
                    w.WritePropertyName("response_format");
                    w.WriteStartObject();
                    w.WriteString("type", "json_object");
                    w.WriteEndObject();
                }
                else
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(js);
                        w.WritePropertyName("response_format");
                        w.WriteRawValue(doc.RootElement.GetRawText());
                    }
                    catch (JsonException)
                    {
                        throw new InvalidOperationException(
                            "llm_completion: for openai_chat, json_schema must be `json_object` or valid JSON for response_format");
                    }
                }
            }

            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    static void WriteLlmOptionalTemperature(Utf8JsonWriter w, LlmCompletionAction action, RunVariables vars)
    {
        if (string.IsNullOrWhiteSpace(action.Temperature))
            return;
        var t = RunVariables.Interpolate(action.Temperature, vars).Trim();
        if (t.Length == 0)
            return;
        if (!double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)
            && !double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out d))
        {
            throw new InvalidOperationException($"llm_completion: temperature must be a number, got `{t}`");
        }

        w.WriteNumber("temperature", d);
    }

    static string LlmCombineUserContent(LlmCompletionAction action, RunVariables vars)
    {
        var user = action.User is null ? "" : RunVariables.Interpolate(action.User, vars);
        var doc = action.Document is null ? "" : RunVariables.Interpolate(action.Document, vars);
        if (user.Length == 0 && doc.Length == 0)
            return "";
        if (user.Length == 0)
            return doc;
        if (doc.Length == 0)
            return user;
        return user + "\n\n---\n\n" + doc;
    }

    static bool LlmHasAnyPromptContent(LlmCompletionAction action, RunVariables vars)
    {
        if (!string.IsNullOrWhiteSpace(action.System) && RunVariables.Interpolate(action.System, vars).Trim().Length > 0)
            return true;
        if (!string.IsNullOrWhiteSpace(action.User) && RunVariables.Interpolate(action.User, vars).Trim().Length > 0)
            return true;
        if (!string.IsNullOrWhiteSpace(action.Document) && RunVariables.Interpolate(action.Document, vars).Trim().Length > 0)
            return true;
        return false;
    }

    static string FormatResponseHeaders(HttpResponseMessage response)
    {
        var sb = new StringBuilder();
        foreach (var h in response.Headers)
        {
            foreach (var v in h.Value)
                sb.AppendLine($"{h.Key}: {v}");
        }
        foreach (var h in response.Content.Headers)
        {
            foreach (var v in h.Value)
                sb.AppendLine($"{h.Key}: {v}");
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Parses media type for <see cref="StringContent"/> (strip parameters beyond charset handled by overload).</summary>
    static string ParseMediaType(string contentType)
    {
        var s = contentType.Trim();
        var semi = s.IndexOf(';');
        return semi < 0 ? s : s[..semi].Trim();
    }

    static IEnumerable<(string Name, string Value)> ParseCustomHeaders(string? raw, RunVariables vars)
    {
        if (string.IsNullOrWhiteSpace(raw))
            yield break;
        foreach (var line in raw.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;
            var colon = trimmed.IndexOf(':');
            if (colon <= 0)
                continue;
            var name = trimmed[..colon].Trim();
            var value = RunVariables.Interpolate(trimmed[(colon + 1)..].Trim(), vars);
            if (name.Length > 0)
                yield return (name, value);
        }
    }

    static HttpMethod ResolveHttpMethod(string? method)
    {
        var m = (method ?? "get").Trim();
        if (m.Length == 0)
            m = "get";
        return m.ToLowerInvariant() switch
        {
            "get" => HttpMethod.Get,
            "post" => HttpMethod.Post,
            "put" => HttpMethod.Put,
            "patch" => HttpMethod.Patch,
            "delete" => HttpMethod.Delete,
            "head" => HttpMethod.Head,
            "options" => HttpMethod.Options,
            "connect" => HttpMethod.Connect,
            "trace" => HttpMethod.Trace,
            _ => throw new InvalidOperationException(
                $"http_request: unsupported method `{method}` (use get, post, put, patch, delete, head, options, connect, trace)"),
        };
    }

    static string ShowMessage(ShowMessageAction sm, RunVariables vars)
    {
        var title = RunVariables.Interpolate(sm.Title, vars);
        var text = RunVariables.Interpolate(sm.Text, vars);
        NativeDialogs.RunShowMessage(title, text, sm.TimeoutSeconds);
        var timeoutNote = sm.TimeoutSeconds is > 0 and var t ? $"timeout={t}s" : "until closed";
        return $"show_message: \"{title}\" ({timeoutNote})";
    }

    static string ShowGetUserInput(ShowGetUserInputAction ui, RunVariables vars)
    {
        var userVar = ui.UserInputVariable.Trim();
        var buttonVar = ui.ButtonPressedVariable.Trim();
        if (string.IsNullOrEmpty(userVar) || string.IsNullOrEmpty(buttonVar))
            throw new InvalidOperationException("show_get_user_input: output variable names must not be empty");
        var title = RunVariables.Interpolate(ui.Title, vars);
        var message = RunVariables.Interpolate(ui.Message, vars);
        var defaultValue = RunVariables.Interpolate(ui.DefaultValue, vars);
        var result = NativeDialogs.RunShowGetUserInput(title, message, defaultValue, ui.InputKind, ui.Topmost ?? false);
        vars[userVar] = result.UserInput;
        vars[buttonVar] = result.ButtonPressed;
        return $"show_get_user_input: button={result.ButtonPressed} user_input=\"{result.UserInput}\"";
    }

    static string ShowSelectFromList(ShowSelectFromListAction sl, RunVariables vars)
    {
        var itemVar = sl.SelectedItemVariable.Trim();
        var indexVar = sl.SelectedIndexVariable.Trim();
        var buttonVar = sl.ButtonPressedVariable.Trim();
        if (string.IsNullOrEmpty(itemVar) || string.IsNullOrEmpty(indexVar) || string.IsNullOrEmpty(buttonVar))
            throw new InvalidOperationException("show_select_from_list: output variable names must not be empty");

        var title = RunVariables.Interpolate(sl.Title, vars);
        var message = RunVariables.Interpolate(sl.Message, vars);
        var items = SelectFromListItems.Parse(sl.List, vars);
        var result = NativeDialogs.RunShowSelectFromList(
            title,
            message,
            items,
            sl.Topmost ?? false,
            sl.LimitToList ?? true,
            sl.AllowEmptySelection ?? false,
            sl.AllowMultipleSelections ?? false);

        vars[itemVar] = result.SelectedItem;
        vars[indexVar] = result.SelectedIndex;
        vars[buttonVar] = result.ButtonPressed;
        return $"show_select_from_list: button={result.ButtonPressed} item=\"{result.SelectedItem}\" index={result.SelectedIndex}";
    }

    static string SetVariable(SetVariableAction sv, RunVariables vars)
    {
        var name = sv.Variable.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("set_variable: variable name is empty");
        var resolved = RunVariables.Interpolate(sv.Value, vars);
        vars[name] = resolved;
        return $"set_variable: `{name}` = {resolved}";
    }
}
