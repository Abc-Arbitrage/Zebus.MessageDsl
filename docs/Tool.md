
# .NET Tool

We offer a .NET CLI tool to generate code from `.msg` files. This tool can be installed either globally or locally within your project.

To install the tool globally:

```bash
dotnet tool install -g Zebus.MessageDsl.Tool
```

After installation, the tool will be accessible as `messagedsl` (on Linux, you may need to refresh your environment variables). It supports the following options:

- `--namespace`: Sets the default namespace for the generated files (optional).
- `--format`: Specifies the output format (`csharp` or `proto`). Defaults to `proto`.

The tool accepts a path to a `.msg` file as an argument. If no argument is provided, it will read the `.msg` file from the standard input.
