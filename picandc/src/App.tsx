import { useState } from 'react'
import './App.css'

interface RunResult {
  output: string;
  error: string;
}

function App() {
  const [data, setData] = useState<RunResult>();
  const [running, setRunning] = useState(false);

  function fetchData(script: string) {
    setRunning(true);
    setData({ output: "running", error: "running"});
    fetch(`/run/${script}.sh`)
      .then(r => r.json())
      .then((data: RunResult) => setData(data))
      .catch(error => setData({ output: "", error: error }))
      .finally(() => setRunning(false));
  }

  return (
    <>
      <button onClick={() => fetchData('test')}>Fetch data</button>
      <button onClick={() => fetchData('test-1')}>Fetch data with error</button>
      <button onClick={() => fetchData('sleep')}>Sleep</button>
      <button onClick={() => setData(undefined)}>Clear</button>
      {running ? <p>Running...</p> : <>
        {data === undefined ?
          <p>Please press the button already!</p>
          :
          <>
            <pre className="output">
              {data.output === '' ? "(No output)" : data.output}
            </pre>
            {data.error !== '' && <pre className="error">{data.error}</pre>}
          </>
        }
      </>}
    </>
  )
}

export default App
