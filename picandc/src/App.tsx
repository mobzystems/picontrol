import { useEffect, useState } from 'react'
import './App.css'

interface RunResult {
  output: string;
  error: string;
  exception: string; // '' is OK
}

export default function App() {
  const [scripts, setScripts] = useState<string[]>();

  useEffect(() => {
    if (scripts === undefined) {
      fetch(`/run/`)
        .then(r => r.json())
        .then((data: string[]) => setScripts(data))
        .catch(error => console.log(error))
        ;
    }
  }, []);

  return (<>
    <div className="app">
      {scripts === undefined ?
        <div id="content"><p>Loading...</p></div>
        :
        <Main scripts={scripts}></Main>
      }
    </div>
  </>);
}

function Main(props: {
  scripts: string[]
}) {
  const [currentScript, setCurrentScript] = useState('');
  const [currentTab, setCurrentTab] = useState<'Output' | 'Error' | 'Exception'>('Output');

  const [state, setState] = useState<{ running: boolean, result: RunResult | undefined }>({ running: false, result: undefined });

  async function fetchData(script: string) {
    setState({ running: true, result: undefined });
    setCurrentTab('Output');

    try {
      var r = await fetch(`/run/${script}`);
      if (!r.ok) {
        console.log(r.status);
        setState({ running: false, result: { output: "", error: "", exception: `Server returned status ${r.status}` } });
      } else {
        var data = await r.json() as RunResult;
        setState({ running: false, result: data });
      }
    } catch (error: any) {
      console.log("Error occurred " + error);
      setState({ running: false, result: { output: "", error: "", exception: error.toString() } });
    }
  }

  return (
    <>
      <div id="top">
        <div className="field has-addons">
          <p className="control is-expanded">
            <span className="select is-fullwidth">
              <select disabled={state.running} value={currentScript} onChange={e => setCurrentScript(e.target.value)}>
                <option value={''}>Choose a script</option>
                {props.scripts.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
            </span>
          </p>
          <p className="control">
            <button className={`button is-success${state.running ? ' is-loading' : ''}`} disabled={state.running || currentScript === ''} onClick={() => fetchData(currentScript!)}>Run</button>
          </p>
          <p className="control">
            <button className="button is-danger" disabled={state.running || state.result === undefined} onClick={() => setState({ running: false, result: undefined })}>Clear</button>
          </p>
        </div>
      </div>

      <div id="content">
        {state.running
          ?
          <p>Running...</p>
          : <>
            {state.result === undefined ?
              <>
              {/* <p>Please select a command.</p> */}
              </>
              :
              <>
                <div id="messages">
                  {state.result.exception !== '' &&
                    <article className="message is-danger">
                      {/* <div className="message-header">There was an exception</div> */}
                      <div className="message-body">{state.result.exception}</div>
                    </article>
                  }

                  {state.result.error !== '' &&
                    <article className="message is-warning">
                      {/* <div className="message-header">There was an error output</div> */}
                      <div className="message-body">See the Error tab for errors</div>
                    </article>
                  }
                </div>

                <div className="tabs">
                  <ul>
                    <li onClick={() => setCurrentTab('Output')} className={currentTab === 'Output' ? 'is-active' : ''}><a>Output</a></li>
                    <li onClick={() => setCurrentTab('Error')} className={currentTab === 'Error' ? 'is-active' : ''}><a>Error</a></li>
                    {/* <li onClick={() => setCurrentTab('Exception')} className={currentTab === 'Exception' ? 'is-active' : ''}><a>Exception</a></li> */}
                  </ul>
                </div>

                <div className="scroller">

                  {currentTab === 'Output' && <>
                    {state.result.output === '' ?
                      <p>There was no output.</p>
                      : <pre className="">
                        {state.result.output === '' ? "(No output)" : state.result.output}
                      </pre>
                    }
                  </>
                  }

                  {currentTab === 'Error' && <>
                    {state.result.error === '' ?
                      <p>There were no errors.</p>
                      :
                      <pre className="has-text-danger">{state.result.error}</pre>
                    }
                  </>
                  }
                  {/* {currentTab === 'Exception' &&
                  <>
                    {state.result.exception === '' ? <p>The command completed uccessfuly</p> : <p>{state.result.exception}</p>}
                  </>
                } */}

                </div>
              </>
            }
          </>}
      </div>
    </>
  )
}