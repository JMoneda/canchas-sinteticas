export default function ErrorMessage({ error }) {
  if (!error) return null
  return (
    <div className="banner-error" style={{ marginTop: '0.75rem' }}>
      {error.message || 'Ha ocurrido un error inesperado.'}
    </div>
  )
}
