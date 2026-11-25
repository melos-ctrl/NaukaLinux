import { marked } from 'marked'
import DOMPurify from 'dompurify'

// GitHub-flavored Markdown; pojedynczy Enter = nowa linia (tak jak mentorzy piszą w textarea)
marked.use({ gfm: true, breaks: true })

// Linki z treści lekcji otwieramy w nowej karcie, bez dostępu do window.opener
DOMPurify.addHook('afterSanitizeAttributes', (node) => {
  if (node.tagName === 'A') {
    node.setAttribute('target', '_blank')
    node.setAttribute('rel', 'noopener noreferrer')
  }
})

/**
 * Zamienia Markdown na bezpieczny HTML.
 * Treść pisze mentor, a czytają ją wszyscy kursanci, więc wynik ZAWSZE przechodzi
 * przez DOMPurify - bez tego <script> albo onerror="..." w treści lekcji byłby atakiem XSS.
 */
export const renderMarkdown = (source?: string | null): string => {
  if (!source) return ''
  const html = marked.parse(source, { async: false })
  return DOMPurify.sanitize(html, {
    // obrazki i iframe'y celowo wyłączone - wideo ma osobny blok
    FORBID_TAGS: ['img', 'iframe', 'style', 'form', 'input'],
  })
}
