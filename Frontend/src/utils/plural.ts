export function polishPlural(count: number, one: string, few: string, many: string): string {
  const n = Math.abs(count)
  if (n === 1) return one

  const lastDigit = n % 10
  const lastTwoDigits = n % 100
  if (lastDigit >= 2 && lastDigit <= 4 && (lastTwoDigits < 12 || lastTwoDigits > 14)) return few

  return many
}

export const reviewsLabel = (count: number) =>
  `${count} ${polishPlural(count, 'opinia', 'opinie', 'opinii')}`

export const starsLabel = (count: number) =>
  `${count} ${polishPlural(count, 'gwiazdka', 'gwiazdki', 'gwiazdek')}`
