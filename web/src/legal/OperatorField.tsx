import { isPlaceholder, operator } from './operator'

export function OperatorField({ value }: { value: string }) {
  return isPlaceholder(value) ? <mark className="placeholder">{value}</mark> : <>{value}</>
}

export function OperatorEmail() {
  const { email } = operator
  return isPlaceholder(email) ? <OperatorField value={email} /> : <a href={`mailto:${email}`}>{email}</a>
}

export function OperatorAddress() {
  return (
    <address>
      <OperatorField value={operator.name} />
      <br />
      <OperatorField value={operator.street} />
      <br />
      <OperatorField value={operator.city} />
      <br />
      {operator.country}
    </address>
  )
}
