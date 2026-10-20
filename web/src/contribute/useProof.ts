import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { ProofSolution } from '../api/types'
import { solve } from './proof'

/**
 * Fetches and solves a proof-of-work challenge as soon as a form opens, so it is usually done by the time the sender
 * presses send. `renew` starts over after a solution was used or turned down.
 */
export function useProof(active: boolean) {
  const [solution, setSolution] = useState<ProofSolution | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [round, setRound] = useState(0)

  useEffect(() => {
    if (!active) return
    let cancelled = false
    let worker: Worker | null = null

    api
      .challenge()
      .then(
        (challenge) =>
          new Promise<ProofSolution>((resolve, reject) => {
            if (typeof Worker === 'undefined') {
              solve(challenge).then(resolve, reject)
              return
            }
            worker = new Worker(new URL('./proof.worker.ts', import.meta.url), { type: 'module' })
            worker.onmessage = (event: MessageEvent<{ solution?: ProofSolution; error?: string }>) =>
              event.data.solution ? resolve(event.data.solution) : reject(new Error(event.data.error))
            worker.onerror = () => reject(new Error('Der Spam-Schutz ließ sich nicht starten.'))
            worker.postMessage(challenge)
          }),
      )
      .then(
        (result) => !cancelled && setSolution(result),
        (reason: Error) => !cancelled && setError(reason.message),
      )
      .finally(() => worker?.terminate())

    return () => {
      cancelled = true
      worker?.terminate()
    }
  }, [active, round])

  return {
    solution,
    error,
    renew: () => {
      setSolution(null)
      setError(null)
      setRound((r) => r + 1)
    },
  }
}
