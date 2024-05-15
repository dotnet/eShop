# AI Batch Evaluator

## Overview

AI Batch Evaluator is a sample command-line application designed for running LLM evaluations for the eShop Chat Bot. It leverages various components from Microsoft Semantic Kernel and Microsoft SK Eval for evaluating coherence and relevance of the provided data.

It aims to offer a proof-of-concept batch evaluation, but feel free to extend it to the specific needs of your Responsible AI process.

## Prerequisites

- See the [eShop pre-requisites](/README.md).

## Installation

Build the application using .NET CLI:

```bash
cd tests/AI.BatchEvals
dotnet build
```

or simply run the application, example:

```bash
cd tests/AI.BatchEvals
dotnet run -- run
```

## Configuration

The application supports environment variables for configuring Azure AI or OpenAI chat completion services:

- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Connection string for Application Insights (optional).
- `OTLP_ENDPOINT`: OpenTelemetry Collector endpoint url (optional).
- `ESHOP_TESTS_AI_COMPLETION_TYPE`: Set to "openai" for OpenAI chat completion or "azureopenai" for Azure OpenAI  endpoints(optional). You can use Ollama and `openai` for using local models.
- `AZURE_AI_MODEL`, `AZURE_AI_ENDPOINT`, `AZURE_AI_KEY`: Configuration for Azure AI chat completion.
- `ESHOP_AI_MODEL`, `ESHOP_AI_ENDPOINT`, `ESHOP_AI_KEY`: Configuration for OpenAI chat completion.

## Usage

The application provides a set of command-line options to configure the evaluation process:

- `--debug`: Enable debug logging. If Application Insights connection string is enabled it will send the logs to Azure Monitor as well.
- `--input <file>`: Specify the input data JSONL file to process (required).
- `--format <format>`: Specify the format of the output. Options: csv, tsv, json (required).

To run standard evaluations, execute the following command:

```bash
dotnet run -- --input <file.jsonl> --format <format>
```

Replace `<file.json>` with the path to your input data file and `<format>` with the desired output format.

## Output

The application generates evaluation results based on the provided input data and chosen output format. Results are printed to the console. If you want to redirect the output to a file use the standard shell (ex. PowerShell or bash) mechanisms. Example:

```bash
dotnet run -- --input assets/tinybatch.jsonl --format csv > results.csv
```

The current sample provide [two batches of questions to play with](assets/).

## Extending

As a proof-of-concept there are some features let out. See [dotnet ai-samples](https://github.com/dotnet/ai-samples) for more in-deep samples.

We are also intentionally using a [custom evaluator](RelevanceEval.cs) with a [custom prompt](_prompts/relevance/skprompt.txt) and a default evaluator (`CoherenceEval`). See [Evaluation and monitoring metrics for generative AI](https://learn.microsoft.com/en-us/azure/ai-studio/concepts/evaluation-metrics-built-in?tabs=warning) for reference.

## References

* [Evaluation and monitoring metrics for generative AI](https://learn.microsoft.com/en-us/azure/ai-studio/concepts/evaluation-metrics-built-in?tabs=warning).
* [dotnet AI Samples](https://github.com/dotnet/ai-samples).
* [dotnet LLM Evaluation samples](https://github.com/microsoft/dotnet-llm-eval-samples/).