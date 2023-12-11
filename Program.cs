using System.CommandLine;

IEnumerable<string> codeFiles;
IEnumerable<string> sortedFiles;
var currentDirectory = Directory.GetCurrentDirectory();
string[] languageList = { "ipynb", "cpp", "js", "ts", "py", "cs", "all" };

void writeColorLine(string msg, ConsoleColor color = ConsoleColor.Blue)
{
    Console.ForegroundColor = color;
    Console.WriteLine(msg);
    Console.ResetColor();
}

var outputOption = new Option<FileInfo>("--output", "file path for output");
outputOption.AddAlias("-o");

var noteOption = new Option<Boolean>("--note", "code source as note");
noteOption.AddAlias("-n");

var langOption = new Option<IEnumerable<String>>("--lang", "code language") { IsRequired = true }.FromAmong(languageList);
langOption.AddAlias("-l");
langOption.AllowMultipleArgumentsPerToken = true;

var authorOption = new Option<String>("--author", "code's author");
authorOption.AddAlias("-a");

var removeEmptyLinesOption = new Option<Boolean>("--remove-empty-lines", "remove empty lines from the source file");
removeEmptyLinesOption.AddAlias("-rel");

var sortOption = new Option<string>(
    name: "--sort",
    description: "The order of copying the code files",
    getDefaultValue: () => "abc")
.FromAmong("lang", "abc");
sortOption.AddAlias("-s");

var bundle = new Command("bundle", "bundle command");

bundle.AddOption(outputOption);
bundle.AddOption(langOption);
bundle.AddOption(noteOption);
bundle.AddOption(authorOption);
bundle.AddOption(removeEmptyLinesOption);
bundle.AddOption(sortOption);

bundle.SetHandler((output, rel, lang, note, author, sort) =>
{
    //-l option
    #region
    if (lang.Any(l => l == "all"))
    {
        codeFiles = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories).Where(
        file => languageList.Any(l => file.EndsWith("." + l))).ToArray();
    }

    else
    {
        codeFiles = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories).Where(
        file => lang.Any(l => file.EndsWith(l))).ToArray();
    }

    #endregion
    try
    {
        if (!File.Exists(output.FullName))
        {
            var outputFile = File.Create(output.FullName);
            outputFile.Close();
        }
        //-s opt
        #region
        Console.WriteLine("sort-------------------{0}", sort);
        if (sort == "lang")
        {
            sortedFiles = codeFiles.OrderBy(f => Path.GetExtension(f));
        }
        else if (sort == "abc")
        {
            sortedFiles = codeFiles.OrderBy(file => Path.GetFileName(file));
        }
        else
        {
            sortedFiles = codeFiles;
        }
        #endregion

        //since i using 'using' which takes care of closing stream i dont need to use writer.close()
        using (StreamWriter writer = new StreamWriter(output.FullName))
        {
            // -a opt
            if (author != null)
                writer.WriteLine("By {0}\n", author);
            foreach (var file in sortedFiles)
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    Console.WriteLine(Path.GetFileName(file));
                    // -rel opt
                    #region
                    if (rel)
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                writer.WriteLine(line);
                        }
                    }
                    else
                    {
                        string content = File.ReadAllText(file);
                        writer.WriteLine(content);
                    }
                    #endregion
                    // -n opt
                    if (note)
                    {
                        writer.WriteLine("\n" + file + "\n");
                    }

                }
            }
        }
    }

    catch (DirectoryNotFoundException e)
    {
        writeColorLine("Error path is invalid", ConsoleColor.Red);
    }
    catch (NullReferenceException e)
    {
        writeColorLine("Output option missing", ConsoleColor.Red);
    }
    catch (UnauthorizedAccessException e)
    {
        writeColorLine("Access to this path is denied", ConsoleColor.Red);
    }

}, outputOption, removeEmptyLinesOption, langOption, noteOption, authorOption, sortOption);


var rsp = new Command("create-rsp", "Generating a response file based on your input");

rsp.SetHandler(() =>
{

    var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    using (StreamWriter writer = File.CreateText(Path.Combine(userPath + "/Music/") + "args.rsp"))
    {
        writeColorLine("Hi lets start");
        Thread.Sleep(1500);

        writeColorLine("What's your name?\n (Your name will appear at the top of the file)");
        writer.WriteLine("-a " + Console.ReadLine());

        writeColorLine("Path of the output file:\n (it can be a path or a name, if it is a name - the file will be created in this folder)");
        var validPath = false;
        do
        {
            var path = Console.ReadLine();
            string parentDirectory = Path.GetDirectoryName(path);
            if (Directory.Exists(parentDirectory))
            {
                writer.WriteLine("-o " + path);
                validPath = true;
            }
            else
            {
                writeColorLine("Path does not exist or is not accessible, Type another one", ConsoleColor.Red);
                validPath = false;
            }
        }
        while (!validPath);

        writeColorLine("Do you want to remove blank lines from the file? y/n");
        if (Console.ReadLine() == "y")
        {
            writer.WriteLine("-rel");
        }
        writeColorLine("Do you want a note on each file with the code source? y/n");
        if (Console.ReadLine() == "y")
        {
            writer.WriteLine("-n");
        }
        writeColorLine("By default, the files are displayed in alphabetical order if you want to order them by their language type y\n otherwise n");

        if (Console.ReadLine() == "y")
        {
            writer.WriteLine("-s lang");
        }
        writeColorLine("Type languages you want to display files written by these languages ​​(with a space between them) from the list below: ");
        foreach (var l in languageList)
        {
            Console.Write(l + " ");
        }
        writeColorLine("If you type 'all' all files will be displayed");
        var langs = Console.ReadLine();
        if (langs == "all" | langs == "'all'")
        {
            writer.WriteLine("-l all");
        }
        else
            writer.WriteLine("-l " + langs);
    }
    Console.WriteLine("Your response file in : " + Path.Combine(userPath + "/Music/") + "args.rsp");
});


var rootCommand = new RootCommand("root command");
rootCommand.AddCommand(bundle);
rootCommand.AddCommand(rsp);

rootCommand.InvokeAsync(args);