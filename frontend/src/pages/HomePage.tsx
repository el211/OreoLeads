import { Button } from '@/components/ui/button'

export function HomePage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-6 p-8">
      <div className="text-center">
        <h1 className="text-4xl font-bold tracking-tight text-foreground">
          OreoLeads
        </h1>
        <p className="mt-2 text-lg text-muted-foreground">
          CRM de prospection pour Oreo Studios
        </p>
      </div>
      <div className="flex gap-4">
        <Button>Se connecter</Button>
        <Button variant="outline">Documentation API</Button>
      </div>
    </div>
  )
}
