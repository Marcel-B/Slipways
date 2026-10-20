import type { ProofChallenge } from '../api/types'
import { solve } from './proof'

// Solves in the background, so the page stays responsive while the sender types.
self.onmessage = async (event: MessageEvent<ProofChallenge>) => {
  try {
    self.postMessage({ solution: await solve(event.data) })
  } catch (error) {
    self.postMessage({ error: (error as Error).message })
  }
}
