//import { dotnet } from 'wasmsample'
//const dotnet = import('wasmsample');
//const dotnet = __non_webpack_require__('./dotnetapp/dotnet.js');
//const dotnet = eval('require(\'./dotnetapp/dotnet.js\')'); 
//const dotnet = null;



async function main()
{
  const module = await import('dotnet');
  //const dotnet = await import('./publish/dotnet.js');
  //const dotnet = eval((await (await fetch('./AppBundle/_framework/dotnet.js')).text()));
  console.log(module.dotnet);

  const is_browser = typeof window != "undefined";
  if (!is_browser) throw new Error(`Expected to be running in a browser`);

  const { setModuleImports, getAssemblyExports, getConfig } = 
    await module.dotnet.create();

  setModuleImports("main.js", {
    window: {
      location: {
        href: () => globalThis.window.location.href
      }
    }
  });

  const config = getConfig();
  const exports = await getAssemblyExports(config.mainAssemblyName);
  console.log(exports);
  const text = exports.dotnet_wasm_sample.MyClass.Greeting();
  console.log(text);

  document.getElementById("out").innerHTML = text;
  await dotnet.run();
}

main();