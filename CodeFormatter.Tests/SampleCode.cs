using CodeFormatter.Models;

namespace CodeFormatter.Tests;

/// <summary>
/// A small valid program per language, with enough in it for most options to have something to do.
/// </summary>
internal static class SampleCode
{
    public static string For(Language language) => Samples[language];

    private static readonly Dictionary<Language, string> Samples = new()
    {
        [Language.Python] = "import os\ndef f(a,b):\n    x=[1,2,3]\n    return 'a'+\"b\"\n",
        [Language.JavaScript] = "import {b,a} from 'm';\nconst f=(x)=>{if(x){return [1,2,3]}else{return {a:1,b:'s'}}}\nclass A{m(){}}\n",
        [Language.TypeScript] = "import {b,a} from 'm';\ninterface I{a:number;b:string}\nconst f=(x:number):I=>{if(x){return {a:1,b:'s'}}return {a:2,b:\"t\"}}\nenum E{A,B}\n",
        [Language.Json] = "{\"a\":1,\"b\":[1,2,3],\"c\":{\"d\":null}}",
        [Language.Markdown] = "# Title\n\nSome *text* and __bold__ text that goes on for a while so that wrapping has something to do here.\n\n* one\n* two\n\n1. a\n2. b\n\n```js\nconst x=1\n```\n",
        [Language.Toml] = "[package]\nname=\"x\"\nversion = \"1\"\n\n[dependencies]\nb = \"1\"\na = { version = \"1\", features = [\"x\",\"y\"] }\n",
        [Language.Css] = "@import 'a.css';\na,b{color:RED;margin:0 0 0 0;background:url(x.png)}\n@media (min-width:100px){a{color:#FFF}}\n",
        [Language.Scss] = "$c:red;\n@mixin m($a,$b){color:$a}\na{&:hover{color:$c}\n@include m(1,2);b{margin:0}}\n",
        [Language.Less] = "@c:red;\n.m(@a,@b){color:@a}\na{&:hover{color:@c}\n.m(1,2);b{margin:0}}\n",
        [Language.Html] = "<!DOCTYPE html>\n<html><head><title>t</title><style>a{color:red}</style></head>\n<body><div class=\"a\" id='b' hidden><p>text <b>bold</b></p><br><img src=\"x.png\"></div>\n<script>const x=1</script></body></html>\n",
        [Language.Vue] = "<template><div :class=\"{a:b}\" v-for=\"i in items\" @click=\"f(i)\"><p v-if=\"x\">{{ i }}</p></div></template>\n<script>export default {data(){return {x:1}}}</script>\n<style scoped>a{color:red}</style>\n",
        [Language.Svelte] = "<script>let x=1;</script>\n{#if x}<div class=\"a\" on:click={()=>x++}>{x}</div>{:else}<p>no</p>{/if}\n<style>a{color:red}</style>\n",
        [Language.Astro] = "---\nconst x=1;\n---\n<div class=\"a\">{x}</div>\n<style>a{color:red}</style>\n",
        [Language.Yaml] = "a: 1\nb:\n  - x\n  - 'y'\n  - {k: v}\nc: [1,2,3]\n# comment\nd: \"s\"\n",
        [Language.GraphQL] = "query Q($id:ID!,$n:Int=1){user(id:$id,first:$n){id,name,...F}}\nfragment F on User{email}\ntype T implements A&B{f(a:Int,b:String):Int @deprecated}\nenum E{A,B}\n",
        [Language.Dockerfile] = "FROM node:20\nRUN apt-get update && apt-get install -y curl\nCOPY . /app\nCMD [\"node\",\"x.js\"]\n",
        [Language.Java] = "import java.util.List;\nimport java.util.ArrayList;\n/** doc */\npublic class A{void m(){List<String> l=new ArrayList<>();String s=\"x\";}}\n",
        [Language.Sql] = "select a,b,count(*) as n from Foo f join Bar b on f.id=b.id where a=1 and b in (1,2) group by a,b order by n desc\n",
        [Language.C] = "#include <stdio.h>\nstruct s{int a;char *b;};\nint main(int argc,char **argv){int x=1;if(x){return 0;}else{return 1;}for(int i=0;i<3;i++)x+=i;switch(x){case 1:break;default:break;}}\n",
        [Language.Cpp] = "#include <vector>\n#include <algorithm>\nnamespace n{class A:public B{public:A():x(1){}private:int x;};}\ntemplate<typename T>T f(const T& a,T* b){if(a)return *b;return a;}\n",
        [Language.CSharpFormatted] = "using System;\nnamespace N{class A{void M(){var x=new[]{1,2,3};if(x.Length>0){Console.WriteLine(\"a\");}}}}\n",
        [Language.Go] = "package main\n\nimport \"fmt\"\n\nfunc f(a int, b int) int {\n\tfmt.Println(0777)\n\treturn a+b\n}\n",
        [Language.Assembly] = "TEXT ·f(SB),NOSPLIT,$0\n\tMOVQ a+0(FP),AX\n\tRET\n",
        [Language.Shell] = "#!/bin/bash\nfoo() {\nif [ -n \"$a\" ] &&\n[ -n \"$b\" ]; then\necho hi >out.txt\nfi\ncase $x in\na) echo a ;;\nesac\n}\n",
        [Language.Lua] = "local a = require('a')\nlocal function f (x)\n  if x then return end\n  print 'hello'\n  call{ 1, 2 }\nend\n",
        [Language.R] = "f<-function(a,b=2){\nx=c(1,2,3)\nif(a>1){print('a')}else{print(\"b\")}\nlist(a=1,\n b=2)\n}\n",
        [Language.Delphi] = "program P;\nvar x:Integer;\nbegin\nif x>1 then begin WriteLn('a'); end else WriteLn('b');\nfor x:=1 to 3 do WriteLn(x);\nend.\n",
        [Language.ObjectiveC] = "#import <Foundation/Foundation.h>\n@interface A:NSObject\n-(void)m:(int)x with:(NSString*)s;\n@end\n@implementation A\n-(void)m:(int)x with:(NSString*)s{if(x>1){NSLog(@\"%@\",s);}else{x=x+1;}for(int i=0;i<3;i++){x+=i;}}\n@end\n",
        [Language.Kotlin] = "package p\n\nimport kotlin.math.max\n\nclass A(val a:Int,val b:String){\n  fun f(x:Int,y:Int):Int{\n    val l=listOf(1,2,3)\n    return if(x>y) max(x,y) else l.size\n  }\n}\n",
        [Language.Haskell] = "module M where\nimport Data.List\nf :: Int -> Int\nf x = if x > 1 then x else 0\n",
        [Language.Perl] = "use strict;\nsub f { my ($a,$b)=@_; if($a>1){print \"a\";}else{print 'b';} for(my $i=0;$i<3;$i++){print $i;} my %h=(a=>1,b=>2); return $a+$b; }\n",
        [Language.Php] = "<?php\nnamespace N;\nuse B\\C;\nuse A\\B;\nclass X{public function m($a,$b){$x=array(1,2,3);if($a==1){return \"s\".$b;}else{return (int)$a+1;}}}\n",
        [Language.Matlab] = "function r=f(a,b)\nx=[1,2,3];\nif a>1\nr=a+b;\nelse\nr=x(1);\nend\nend\n",
        [Language.Jinja] = "<ul>\n{% for i in items %}\n<li   class=\"a\" id=\'b\'>{{ i.name }}</li>\n{% endfor %}\n</ul>\n<script>const x=1</script>\n<style>a{color:red}</style>\n",
        [Language.Twig] = "<ul>\n{% for i in items %}\n<li   class=\"a\" id=\'b\'>{{ i.name }}</li>\n{% endfor %}\n</ul>\n<script>const x=1</script>\n<style>a{color:red}</style>\n",
        [Language.Nunjucks] = "<ul>\n{% for i in items %}\n<li   class=\"a\" id=\'b\'>{{ i.name }}</li>\n{% endfor %}\n</ul>\n<script>const x=1</script>\n<style>a{color:red}</style>\n",
        [Language.Vento] = "<ul>\n{{ for i of items }}\n<li   class=\"a\" id='b'>{{ i.name }}</li>\n{{ /for }}\n</ul>\n<script>const x=1</script>\n",
        [Language.Handlebars] = "<ul>\n{{#items}}\n<li   class=\"a\" id=\'b\'>{{name}}</li>\n{{/items}}\n</ul>\n<script>const x=1</script>\n<style>a{color:red}</style>\n",
        [Language.Mustache] = "<ul>\n{{#items}}\n<li   class=\"a\" id=\'b\'>{{name}}</li>\n{{/items}}\n</ul>\n<script>const x=1</script>\n<style>a{color:red}</style>\n",
        [Language.Angular] = "<div   *ngIf=\"x\" class=\"a\" id='b'>\n@if (a) {\n<p>{{y}}</p>\n} @else {\n<br>\n}\n</div>\n",
        [Language.Latex] = "\\documentclass{article}\n\\begin{document}\nSome text that goes on for a while. Another sentence follows here, so wrapping has work to do.\n\\begin{itemize}\n\\item first item\nwith a continuation\n\\end{itemize}\n\\[ a = b + c \\]\n\\end{document}\n",
        [Language.Bibtex] = "@ARTICLE{key1,\n  Title = \"A Title\",\n  author={Smith, J.},\n  YEAR = {2020},\n  month = \"January\",\n  journal = {{Journal}},\n}\n% a comment\n@book{abook, title={B}, year=1999}\n",
        [Language.Typst] = "#import \"a.typ\": c, b\n= Title\nSome   text that goes on. Another sentence.\n\n\n\n#let f(x)=x+1\n#f(  1 )\n",
        [Language.Julia] = "function f(x,y)\nif x>1\nreturn x+y\nend\n[1,2,3]\nend\n",
        [Language.CMake] = "cmake_minimum_required(VERSION 3.10)\nPROJECT(demo)\nadd_executable(app   main.c util.c other.c)\nif(X)\ntarget_link_libraries(app public m)\nendif()\n\n\n\n",
        [Language.Ruby] = "def f a, b\n  x = [1,\n   2]\n  h = {:a=>1, 'b'=>\"c\"}\n  case a\n  when 1 then 2\n  when 10 then 20\n  end\n  foo.bar\n     .baz\nend\n",
    };
}
