1. Добавляем новое окружение для wasm: dotnet workload install wasm-tools
2. Создаём пустой консольный проект
3. Добавляем часть строк в csproj файл:

``` xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>

    <!-- добавляем вот этот кусок -->
    <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>

    <!-- JSExport requires unsafe code -->
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <WasmMainJSPath>main.js</WasmMainJSPath>
    <!-- конец -->
    
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

4. Добавлем файл main.js в корень проекта. Пустой. Нужен для нормальной работы тулсета, думаю в будущем починят и необходимость в нем отпадет.
5. Выполняем dotnet publish -c Release
6. Добавляем frontend через create-react-app

TODO: Выяснить для чего нужен RuntimeIdentifier
TODO: Выяснить для чего нужен AllowUnsafeBlock
TODO: Выяснить для чего нужен WasmMainJSPath
TODO: Почитать про
``` xml
<!-- start of optoinal size optimization -->
<InvariantGlobalization>true</InvariantGlobalization>
<WasmEmitSymbolMap>false</WasmEmitSymbolMap>
<!-- end of size optimization -->
```