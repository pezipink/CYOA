namespace CYOAProvider

open System
open System.IO
open System.Reflection
open System.Collections.Generic
open Samples.FSharp.ProvidedTypes
open CYOAProvider.Data
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations

[<TypeProvider>]
type Provider(config: TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let ns = "CYOAProvider"
    let asm = Assembly.GetExecutingAssembly()

    let createTypes fileName rootTypeName =        
        let typeDict = Dictionary<string, ProvidedTypeDefinition>()
        if not <| File.Exists(fileName) then failwithf "Could not find data file %s" fileName

        let parseLine (line:string) = 
            let split = line.Split('|')
            let choices = split.[2].Split('¿')
            { Key = split.[0]
              Text = split.[1]
              Choices = if choices.Length > 1 then [ for i in 0..2..choices.Length-1 -> (choices.[i],choices.[i+1]) ] else [] }
        
        // read file 
        let data = 
            File.ReadAllLines(fileName)
            |> Array.map parseLine
            |> List.ofArray
        
        //text lookup
        let lookup = dict [for p in data -> (p.Key,p.Text)]

        // all the types need to first exist before putting methods on them as they are point at each other
        data |> List.iter(fun pageEntry -> typeDict.Add(pageEntry.Key,ProvidedTypeDefinition(pageEntry.Key,None,HideObjectMethods=true)))
        
        // now populate them
        data 
        |> List.iter(fun pageEntry ->   
            let t = typeDict.[pageEntry.Key]
            t.AddXmlDoc <| "<summary>"+pageEntry.Text+"</summary>"
            t.AddMember(ProvidedConstructor([],InvokeCode = (fun _ -> <@@ obj() @@> )))
            t.AddMember(ProvidedProperty("- Make a choice:",typeof<obj>,GetterCode = (fun args -> <@@ obj() @@> )))
            t.AddMembers([for (key,text) in pageEntry.Choices -> let p = ProvidedProperty(text,typeDict.[key],GetterCode = (fun args -> <@@ obj() @@> ))
                                                                 p.AddXmlDoc("<summary>"+lookup.[key]+"</summary>");p]))
        let rootType = ProvidedTypeDefinition(asm,ns,rootTypeName,None,HideObjectMethods=true)
        rootType.AddMembers([for kvp in typeDict -> kvp.Value])
        rootType.AddMember(let p = ProvidedProperty("Intro",typeDict.["Intro"],GetterCode = (fun args -> <@@ obj() @@> ))
                           p.AddXmlDoc("<summary>"+lookup.["Intro"]+"</summary>");p)
        rootType.AddMember(ProvidedConstructor([],InvokeCode = (fun _ -> <@@ obj() @@> )))
        rootType
         
    let paramType = ProvidedTypeDefinition(asm, ns, "CYOAProvider", None, HideObjectMethods = true)
    let dataFile = ProvidedStaticParameter("DataFile",typeof<string>)    
    do paramType.DefineStaticParameters([dataFile], fun typeName args -> createTypes (args.[0]:?>string) typeName )
    do this.AddNamespace(ns, [paramType])


[<assembly:TypeProviderAssembly>] 
do()