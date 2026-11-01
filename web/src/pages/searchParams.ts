import { useSearchParams } from 'react-router'

/**
 * The page's query parameters, a setter that drops a parameter once it is empty and keeps the others, and one that
 * drops several at once.
 */
export function useQueryParams(): [
  URLSearchParams,
  (key: string, value: string) => void,
  (...keys: string[]) => void,
] {
  const [params, setParams] = useSearchParams()
  const setParam = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }
  const clearParams = (...keys: string[]) => {
    const next = new URLSearchParams(params)
    for (const key of keys) next.delete(key)
    setParams(next, { replace: true })
  }
  return [params, setParam, clearParams]
}
