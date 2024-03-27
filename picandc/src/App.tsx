import { useEffect, useState } from 'react'
import './App.css'

interface RunResult {
  output: string;
  error: string;
  exception: string; // '' is OK
}

function App() {
  const [scripts, setScripts] = useState<string[]>();
  const [currentScript, setCurrentScript] = useState('');
  const [state, setState] = useState<{ running: boolean, result: RunResult | undefined }>({ running: false, result: undefined });

  async function fetchData(script: string) {
    setState({ running: true, result: undefined });
    try {
    var r = await fetch(`/run/${script}`);
    if (!r.ok) {
      console.log(r.status); 
      setState({ running: false, result: { output: "", error: "", exception: `Server returned status ${r.status}` }});
    } else {
      var data = await r.json() as RunResult;
      setState({ running: false, result: data });
      }
    } catch (error: any) {
      console.log("Error occurred " + error);
      setState({ running: false, result: { output: "", error: "", exception: error.toString() } });
    }
  }

  useEffect(() => {
    if (scripts === undefined) {
      fetch(`/run/`)
      .then(r => r.json())
      .then((data: string[]) => setScripts(data))
      .catch(error => console.log(error))
      ;
    }
  }, []);

  if (scripts === undefined)
    return (<p>Loading...</p>);

  return (
    <>
      <div>
        <select disabled={state.running} value={currentScript} onChange={e => setCurrentScript(e.target.value)}>
          <option value={''}>Choose a script</option>
          {scripts.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <button disabled={state.running || currentScript === ''} onClick={() => fetchData(currentScript!)}>Run</button>
        <button disabled={state.running} onClick={() => setState({ running: false, result: undefined })}>Clear</button>
      </div>
      {state.running
        ?
        <p>Running...</p>
        : <>
          {state.result === undefined ?
            <p>Press the button already!</p>
            :
            <>
              <p>
                Result: {state.result.exception === '' ? 'The command completed successfully.' : state.result.exception}
              </p>
              <p>Output:</p>
              <pre className="output">
                {state.result.output === '' ? "(No output)" : state.result.output}
              </pre>
              {state.result.error !== '' && <>
                <p>Error:</p>
                <pre className="error">{state.result.error}</pre>
              </>}
            </>
          }
        </>}
    </>
  )
}

export default App
