using CodeFormatter.Models;
using CodeFormatter.Services;
using Xunit;

namespace CodeFormatter.Tests;

/// <summary>
/// Integration tests for all supported formatters.
/// These tests verify that each language formatter works correctly with real binaries.
/// </summary>
public class FormatterIntegrationTests
{
    private readonly FormatterService _formatterService;

    public FormatterIntegrationTests()
    {
        var configManager = new ConfigManager();
        var processRunner = new ProcessRunner();
        _formatterService = new FormatterService(configManager, processRunner);
    }

    // ========== Python (Ruff) ==========

    [Fact]
    public async Task Python_FormatsSimpleAssignment()
    {
        var input = "x=1";
        var result = await _formatterService.FormatAsync(input, Language.Python);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Equal("x = 1\n", result.Output);
    }

    [Fact]
    public async Task Python_FormatsFunction()
    {
        var input = "def foo():return 42";
        var result = await _formatterService.FormatAsync(input, Language.Python);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("def foo():", result.Output);
        Assert.Contains("return 42", result.Output);
    }

    // ========== JavaScript (dprint typescript plugin) ==========

    [Fact]
    public async Task JavaScript_FormatsSimpleCode()
    {
        var input = "const x=1;function foo(){return x}";
        var result = await _formatterService.FormatAsync(input, Language.JavaScript);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("const x = 1;", result.Output);
        Assert.Contains("function foo()", result.Output);
    }

    [Fact]
    public async Task JavaScript_FormatsArrowFunction()
    {
        var input = "const add=(a,b)=>a+b";
        var result = await _formatterService.FormatAsync(input, Language.JavaScript);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("const add = (a, b) => a + b", result.Output);
    }

    // ========== TypeScript (dprint typescript plugin) ==========

    [Fact]
    public async Task TypeScript_FormatsWithTypes()
    {
        var input = "const x:number=1;function foo():number{return x}";
        var result = await _formatterService.FormatAsync(input, Language.TypeScript);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("const x: number = 1;", result.Output);
        Assert.Contains("function foo(): number", result.Output);
    }

    [Fact]
    public async Task TypeScript_FormatsInterface()
    {
        var input = "interface User{name:string;age:number}";
        var result = await _formatterService.FormatAsync(input, Language.TypeScript);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("interface User", result.Output);
        Assert.Contains("name: string", result.Output);
    }

    // ========== JSON (dprint json plugin) ==========

    [Fact]
    public async Task Json_FormatsCompactObject()
    {
        var input = "{\"name\":\"test\",\"value\":1}";
        var result = await _formatterService.FormatAsync(input, Language.Json);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("\"name\": \"test\"", result.Output);
    }

    [Fact]
    public async Task Json_FormatsArray()
    {
        var input = "[1,2,3]";
        var result = await _formatterService.FormatAsync(input, Language.Json);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        // dprint may format as single line or multi-line
        Assert.Contains("1", result.Output);
        Assert.Contains("2", result.Output);
        Assert.Contains("3", result.Output);
    }

    // ========== Markdown (dprint markdown plugin) ==========

    [Fact]
    public async Task Markdown_FormatsHeading()
    {
        var input = "# Title";
        var result = await _formatterService.FormatAsync(input, Language.Markdown);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("# Title", result.Output);
    }

    [Fact]
    public async Task Markdown_FormatsList()
    {
        var input = "*item1\n*item2";
        var result = await _formatterService.FormatAsync(input, Language.Markdown);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        // Markdown formatter normalizes list markers
        Assert.Contains("item1", result.Output);
        Assert.Contains("item2", result.Output);
    }

    // ========== TOML (dprint toml plugin) ==========

    [Fact]
    public async Task Toml_FormatsKeyValue()
    {
        var input = "key=\"value\"";
        var result = await _formatterService.FormatAsync(input, Language.Toml);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("key = \"value\"", result.Output);
    }

    [Fact]
    public async Task Toml_FormatsSection()
    {
        var input = "[section]\nkey=\"value\"";
        var result = await _formatterService.FormatAsync(input, Language.Toml);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("[section]", result.Output);
        Assert.Contains("key = \"value\"", result.Output);
    }

    // ========== CSS (dprint malva plugin) ==========

    [Fact]
    public async Task Css_FormatsRule()
    {
        var input = ".class{color:red;margin:0}";
        var result = await _formatterService.FormatAsync(input, Language.Css);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains(".class", result.Output);
        Assert.Contains("color:", result.Output);
    }

    // ========== SCSS (dprint malva plugin) ==========

    [Fact]
    public async Task Scss_FormatsNestedRule()
    {
        var input = ".parent{.child{color:red}}";
        var result = await _formatterService.FormatAsync(input, Language.Scss);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains(".parent", result.Output);
        Assert.Contains(".child", result.Output);
    }

    // ========== HTML (dprint markup_fmt plugin) ==========

    [Fact]
    public async Task Html_FormatsElement()
    {
        var input = "<div><span>test</span></div>";
        var result = await _formatterService.FormatAsync(input, Language.Html);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("<div>", result.Output);
        Assert.Contains("<span>", result.Output);
    }

    // ========== YAML (dprint pretty_yaml plugin) ==========

    [Fact]
    public async Task Yaml_FormatsMapping()
    {
        var input = "name: test\nvalue: 123";
        var result = await _formatterService.FormatAsync(input, Language.Yaml);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("name:", result.Output);
        Assert.Contains("value:", result.Output);
    }

    // ========== GraphQL (dprint pretty_graphql plugin) ==========

    [Fact]
    public async Task GraphQL_FormatsQuery()
    {
        var input = "query{user{name}}";
        var result = await _formatterService.FormatAsync(input, Language.GraphQL);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("query", result.Output);
        Assert.Contains("user", result.Output);
    }

    // ========== Dockerfile (dprint dockerfile plugin) ==========

    [Fact]
    public async Task Dockerfile_FormatsFromInstruction()
    {
        var input = "FROM node:18";
        var result = await _formatterService.FormatAsync(input, Language.Dockerfile);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("FROM node:18", result.Output);
    }

    [Fact]
    public async Task Dockerfile_FormatsRunInstruction()
    {
        var input = "FROM node:18\nRUN npm install";
        var result = await _formatterService.FormatAsync(input, Language.Dockerfile);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("FROM", result.Output);
        Assert.Contains("RUN", result.Output);
    }

    // ========== Error Handling ==========

    [Fact]
    public async Task EmptyInput_ReturnsError()
    {
        var result = await _formatterService.FormatAsync("", Language.Python);

        Assert.False(result.Success);
        Assert.Contains("No code to format", result.Output);
    }

    [Fact]
    public async Task WhitespaceOnlyInput_ReturnsError()
    {
        var result = await _formatterService.FormatAsync("   \n\t  ", Language.Python);

        Assert.False(result.Success);
        Assert.Contains("No code to format", result.Output);
    }

    // ========== Java (google-java-format native binary) ==========

    [Fact]
    public async Task Java_FormatsClass()
    {
        var input = "public class Foo{public static void main(String[] args){}}";
        var result = await _formatterService.FormatAsync(input, Language.Java);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("public class Foo", result.Output);
        Assert.Contains("public static void main", result.Output);
    }

    [Fact]
    public async Task Java_FormatsMethod()
    {
        var input = "public class Test{public int add(int a,int b){return a+b;}}";
        var result = await _formatterService.FormatAsync(input, Language.Java);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("public int add(int a, int b)", result.Output);
        Assert.Contains("return a + b;", result.Output);
    }

    [Fact]
    public async Task Java_FormatsImports()
    {
        var input = "import java.util.List;import java.util.Map;public class Foo{}";
        var result = await _formatterService.FormatAsync(input, Language.Java);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("import java.util.List;", result.Output);
        Assert.Contains("import java.util.Map;", result.Output);
    }

    // ========== SQL (npx sql-formatter) ==========
    // Requires Node.js with sql-formatter installed globally

    [Fact(Skip = "Requires Node.js with sql-formatter installed globally")]
    public async Task Sql_FormatsSelect()
    {
        var input = "select id,name from users where active=true";
        var result = await _formatterService.FormatAsync(input, Language.Sql);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("SELECT", result.Output.ToUpperInvariant());
        Assert.Contains("FROM", result.Output.ToUpperInvariant());
    }

    // ========== C (clang-format) ==========

    [Fact]
    public async Task C_FormatsSimpleFunction()
    {
        var input = "int main(){int x=1;return 0;}";
        var result = await _formatterService.FormatAsync(input, Language.C);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("int main()", result.Output);
        Assert.Contains("int x = 1;", result.Output);
        Assert.Contains("return 0;", result.Output);
    }

    [Fact]
    public async Task C_FormatsStruct()
    {
        var input = "struct Point{int x;int y;};";
        var result = await _formatterService.FormatAsync(input, Language.C);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("struct Point", result.Output);
        Assert.Contains("int x;", result.Output);
        Assert.Contains("int y;", result.Output);
    }

    [Fact]
    public async Task C_FormatsIfStatement()
    {
        var input = "void foo(){if(x>0){y=1;}else{y=2;}}";
        var result = await _formatterService.FormatAsync(input, Language.C);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("if (x > 0)", result.Output);
        Assert.Contains("else", result.Output);
    }

    // ========== C++ (clang-format) ==========

    [Fact]
    public async Task Cpp_FormatsClass()
    {
        var input = "class Foo{public:int bar();private:int x;};";
        var result = await _formatterService.FormatAsync(input, Language.Cpp);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("class Foo", result.Output);
        Assert.Contains("public:", result.Output);
        Assert.Contains("private:", result.Output);
    }

    [Fact]
    public async Task Cpp_FormatsTemplate()
    {
        var input = "template<typename T>T max(T a,T b){return a>b?a:b;}";
        var result = await _formatterService.FormatAsync(input, Language.Cpp);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("template", result.Output);
        Assert.Contains("typename T", result.Output);
    }

    [Fact]
    public async Task Cpp_FormatsNamespace()
    {
        var input = "namespace foo{class Bar{};}";
        var result = await _formatterService.FormatAsync(input, Language.Cpp);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("namespace foo", result.Output);
        Assert.Contains("class Bar", result.Output);
    }

    // ========== Go (gofumpt) ==========

    [Fact]
    public async Task Go_FormatsSimpleFunction()
    {
        var input = "package main\nfunc main(){fmt.Println(\"hello\")}";
        var result = await _formatterService.FormatAsync(input, Language.Go);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("package main", result.Output);
        Assert.Contains("func main()", result.Output);
    }

    [Fact]
    public async Task Go_FormatsStruct()
    {
        var input = "package main\ntype Point struct{X int\nY int}";
        var result = await _formatterService.FormatAsync(input, Language.Go);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("type Point struct", result.Output);
        Assert.Contains("X int", result.Output);
        Assert.Contains("Y int", result.Output);
    }

    [Fact]
    public async Task Go_FormatsImport()
    {
        var input = "package main\nimport \"fmt\"\nfunc main(){fmt.Println(\"hi\")}";
        var result = await _formatterService.FormatAsync(input, Language.Go);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("import \"fmt\"", result.Output);
    }

    // ========== Shell/Bash (shfmt) ==========

    [Fact]
    public async Task Shell_FormatsIfStatement()
    {
        var input = "#!/bin/bash\nif [ $x = 1 ];then\necho hello\nfi";
        var result = await _formatterService.FormatAsync(input, Language.Shell);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("if [", result.Output);
        Assert.Contains("then", result.Output);
        Assert.Contains("fi", result.Output);
    }

    [Fact]
    public async Task Shell_FormatsForLoop()
    {
        var input = "#!/bin/bash\nfor i in 1 2 3;do\necho $i\ndone";
        var result = await _formatterService.FormatAsync(input, Language.Shell);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("for i in", result.Output);
        Assert.Contains("do", result.Output);
        Assert.Contains("done", result.Output);
    }

    [Fact]
    public async Task Shell_FormatsFunction()
    {
        var input = "#!/bin/bash\nmy_func(){\necho hello\n}";
        var result = await _formatterService.FormatAsync(input, Language.Shell);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("my_func()", result.Output);
        Assert.Contains("echo hello", result.Output);
    }

    [Fact]
    public async Task Shell_FormatsCaseStatement()
    {
        var input = "#!/bin/bash\ncase $x in\n1)echo one;;\n2)echo two;;\nesac";
        var result = await _formatterService.FormatAsync(input, Language.Shell);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("case", result.Output);
        Assert.Contains("esac", result.Output);
    }

    // ========== Lua (StyLua) ==========

    [Fact]
    public async Task Lua_FormatsSimpleAssignment()
    {
        var input = "local x=1";
        var result = await _formatterService.FormatAsync(input, Language.Lua);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("local x = 1", result.Output);
    }

    [Fact]
    public async Task Lua_FormatsFunction()
    {
        var input = "function hello(name)print(\"Hello \"..name)end";
        var result = await _formatterService.FormatAsync(input, Language.Lua);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("function hello(name)", result.Output);
        Assert.Contains("print(", result.Output);
        Assert.Contains("end", result.Output);
    }

    [Fact]
    public async Task Lua_FormatsTable()
    {
        var input = "local t={a=1,b=2,c=3}";
        var result = await _formatterService.FormatAsync(input, Language.Lua);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("local t =", result.Output);
        Assert.Contains("a = 1", result.Output);
    }

    // ========== R (Air) ==========

    [Fact]
    public async Task R_FormatsSimpleAssignment()
    {
        var input = "x<-1";
        var result = await _formatterService.FormatAsync(input, Language.R);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("x <- 1", result.Output);
    }

    [Fact]
    public async Task R_FormatsFunction()
    {
        var input = "add<-function(a,b){return(a+b)}";
        var result = await _formatterService.FormatAsync(input, Language.R);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("function(a, b)", result.Output);
        Assert.Contains("return(a + b)", result.Output);
    }

    [Fact]
    public async Task R_FormatsVector()
    {
        var input = "v<-c(1,2,3,4,5)";
        var result = await _formatterService.FormatAsync(input, Language.R);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("v <- c(", result.Output);
    }

    // ========== Delphi/Pascal (pasfmt) ==========

    [Fact]
    public async Task Delphi_FormatsProcedure()
    {
        var input = "procedure Test;begin WriteLn('Hello');end;";
        var result = await _formatterService.FormatAsync(input, Language.Delphi);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("procedure Test;", result.Output);
        Assert.Contains("begin", result.Output);
        Assert.Contains("end;", result.Output);
    }

    [Fact]
    public async Task Delphi_FormatsFunction()
    {
        var input = "function Add(a,b:Integer):Integer;begin Result:=a+b;end;";
        var result = await _formatterService.FormatAsync(input, Language.Delphi);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("function Add", result.Output);
        Assert.Contains("Result :=", result.Output);
    }

    // ========== C# (CSharpier) ==========

    [Fact]
    public async Task CSharp_FormatsClass()
    {
        var input = "public class Test{public void Method(){Console.WriteLine(\"Hello\");}}";
        var result = await _formatterService.FormatAsync(input, Language.CSharpFormatted);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("public class Test", result.Output);
        Assert.Contains("public void Method()", result.Output);
    }

    [Fact]
    public async Task CSharp_FormatsProperty()
    {
        var input = "public class Test{public string Name{get;set;}}";
        var result = await _formatterService.FormatAsync(input, Language.CSharpFormatted);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("public string Name", result.Output);
        Assert.Contains("get;", result.Output);
        Assert.Contains("set;", result.Output);
    }

    [Fact]
    public async Task CSharp_FormatsLinq()
    {
        var input = "var result=items.Where(x=>x>0).Select(x=>x*2).ToList();";
        var result = await _formatterService.FormatAsync(input, Language.CSharpFormatted);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("var result =", result.Output);
        Assert.Contains("Where(x => x > 0)", result.Output);
    }

    // ========== Assembly (asmfmt) ==========

    [Fact]
    public async Task Assembly_FormatsGoAssembly()
    {
        var input = "TEXT ·Add(SB),$0-24\nMOVQ a+0(FP),AX\nMOVQ b+8(FP),BX\nADDQ AX,BX\nRET";
        var result = await _formatterService.FormatAsync(input, Language.Assembly);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("TEXT", result.Output);
        Assert.Contains("MOVQ", result.Output);
        Assert.Contains("RET", result.Output);
    }

    // ========== Objective-C (uncrustify) ==========

    [Fact]
    public async Task ObjectiveC_FormatsInterface()
    {
        var input = "@interface Person:NSObject @property NSString*name;@end";
        var result = await _formatterService.FormatAsync(input, Language.ObjectiveC);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("@interface Person", result.Output);
        Assert.Contains("@property", result.Output);
        Assert.Contains("@end", result.Output);
    }

    [Fact]
    public async Task ObjectiveC_FormatsMethod()
    {
        var input = "@implementation Test -(void)hello{NSLog(@\"Hello\");}@end";
        var result = await _formatterService.FormatAsync(input, Language.ObjectiveC);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("@implementation Test", result.Output);
        Assert.Contains("-(void)hello", result.Output);
    }

    // ========== Kotlin (ktlint) ==========

    [Fact]
    public async Task Kotlin_FormatsFunction()
    {
        var input = "fun main(){println(\"Hello\")}";
        var result = await _formatterService.FormatAsync(input, Language.Kotlin);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("fun main()", result.Output);
        Assert.Contains("println(", result.Output);
    }

    [Fact]
    public async Task Kotlin_FormatsClass()
    {
        var input = "class Person(val name:String,val age:Int){fun greet()=println(\"Hello $name\")}";
        var result = await _formatterService.FormatAsync(input, Language.Kotlin);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("class Person", result.Output);
        Assert.Contains("val name: String", result.Output);
    }

    [Fact]
    public async Task Kotlin_FormatsLambda()
    {
        var input = "val sum={a:Int,b:Int->a+b}";
        var result = await _formatterService.FormatAsync(input, Language.Kotlin);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("val sum =", result.Output);
        Assert.Contains("a: Int", result.Output);
    }

    // ========== Haskell (Ormolu) ==========

    [Fact]
    public async Task Haskell_FormatsFunction()
    {
        var input = "main=putStrLn \"Hello\"";
        var result = await _formatterService.FormatAsync(input, Language.Haskell);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("main = putStrLn", result.Output);
    }

    [Fact]
    public async Task Haskell_FormatsTypeSignature()
    {
        var input = "add::Int->Int->Int\nadd a b=a+b";
        var result = await _formatterService.FormatAsync(input, Language.Haskell);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("add :: Int -> Int -> Int", result.Output);
        Assert.Contains("add a b = a + b", result.Output);
    }

    [Fact]
    public async Task Haskell_FormatsList()
    {
        var input = "nums=[1,2,3,4,5]";
        var result = await _formatterService.FormatAsync(input, Language.Haskell);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("nums = [1, 2, 3, 4, 5]", result.Output);
    }

    // ========== Perl (perltidy) ==========

    [Fact]
    public async Task Perl_FormatsSubroutine()
    {
        var input = "sub hello{my $name=shift;print \"Hello $name\\n\";}";
        var result = await _formatterService.FormatAsync(input, Language.Perl);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("sub hello", result.Output);
        Assert.Contains("my $name", result.Output);
    }

    [Fact]
    public async Task Perl_FormatsHashAccess()
    {
        var input = "my %hash=(a=>1,b=>2);print $hash{a};";
        var result = await _formatterService.FormatAsync(input, Language.Perl);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("my %hash", result.Output);
        Assert.Contains("$hash{a}", result.Output);
    }

    [Fact]
    public async Task Perl_FormatsRegex()
    {
        var input = "if($str=~/pattern/){print \"match\";}";
        var result = await _formatterService.FormatAsync(input, Language.Perl);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("if (", result.Output);
        Assert.Contains("=~", result.Output);
    }

    // ========== PHP (PHP-CS-Fixer) ==========

    [Fact]
    public async Task Php_FormatsClass()
    {
        var input = "<?php class Test{public function hello(){echo \"Hello\";}}";
        var result = await _formatterService.FormatAsync(input, Language.Php);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("class Test", result.Output);
        Assert.Contains("public function hello()", result.Output);
    }

    [Fact]
    public async Task Php_FormatsArray()
    {
        var input = "<?php $arr=array(\"a\"=>1,\"b\"=>2);";
        var result = await _formatterService.FormatAsync(input, Language.Php);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("$arr =", result.Output);
    }

    [Fact]
    public async Task Php_FormatsNamespace()
    {
        var input = "<?php namespace App\\Controllers;class HomeController{}";
        var result = await _formatterService.FormatAsync(input, Language.Php);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("namespace App\\Controllers;", result.Output);
        Assert.Contains("class HomeController", result.Output);
    }

    // ========== MATLAB (MISS_HIT) ==========

    [Fact]
    public async Task Matlab_FormatsFunction()
    {
        var input = "function result=add(a,b)\nresult=a+b;\nend";
        var result = await _formatterService.FormatAsync(input, Language.Matlab);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("function result", result.Output);
        Assert.Contains("result =", result.Output);
    }

    [Fact]
    public async Task Matlab_FormatsForLoop()
    {
        var input = "for i=1:10\ndisp(i);\nend";
        var result = await _formatterService.FormatAsync(input, Language.Matlab);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("for i", result.Output);
        Assert.Contains("disp(i)", result.Output);
        Assert.Contains("end", result.Output);
    }

    [Fact]
    public async Task Matlab_FormatsMatrix()
    {
        var input = "A=[1 2 3;4 5 6;7 8 9];";
        var result = await _formatterService.FormatAsync(input, Language.Matlab);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("A =", result.Output);
    }

    // ========== Ruby (rufo) ==========

    [Fact]
    public async Task Ruby_FormatsMethod()
    {
        var input = "def hello;puts 'Hello';end";
        var result = await _formatterService.FormatAsync(input, Language.Ruby);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("def hello", result.Output);
        Assert.Contains("puts", result.Output);
        Assert.Contains("end", result.Output);
    }

    [Fact]
    public async Task Ruby_FormatsClass()
    {
        var input = "class Person;attr_accessor :name;def initialize(name);@name=name;end;end";
        var result = await _formatterService.FormatAsync(input, Language.Ruby);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("class Person", result.Output);
        Assert.Contains("attr_accessor", result.Output);
        Assert.Contains("def initialize", result.Output);
    }

    [Fact]
    public async Task Ruby_FormatsBlock()
    {
        var input = "[1,2,3].each{|x|puts x}";
        var result = await _formatterService.FormatAsync(input, Language.Ruby);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("[1, 2, 3]", result.Output);
        Assert.Contains(".each", result.Output);
    }

    [Fact]
    public async Task Ruby_FormatsHash()
    {
        var input = "h={a:1,b:2,c:3}";
        var result = await _formatterService.FormatAsync(input, Language.Ruby);

        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("h =", result.Output);
        Assert.Contains("a:", result.Output);
    }
}
