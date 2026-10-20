import type { ProofChallenge, ProofSolution } from '../api/types'

async function sha256(text: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text))
  return Array.from(new Uint8Array(digest), (b) => b.toString(16).padStart(2, '0')).join('')
}

/** Finds the number with SHA-256(salt + number) = challenge; about a second for the server's usual challenge. */
export async function solve(challenge: ProofChallenge): Promise<ProofSolution> {
  for (let number = 0; number <= challenge.maxNumber; number++) {
    if ((await sha256(challenge.salt + number)) === challenge.challenge) {
      return {
        algorithm: challenge.algorithm,
        challenge: challenge.challenge,
        number,
        salt: challenge.salt,
        signature: challenge.signature,
      }
    }
  }
  throw new Error('Der Spam-Schutz ließ sich nicht lösen.')
}
