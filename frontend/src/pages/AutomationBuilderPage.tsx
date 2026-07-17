import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Save, Play, CheckCircle, XCircle, Plus, Trash2, GripVertical, ArrowLeft } from 'lucide-react'
import { useWorkflow, useCreateWorkflow, useUpdateWorkflow, useExecuteWorkflow } from '@/hooks/useAutomation'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { ActionType } from '@/types/automation'

interface StepConfig {
  type: ActionType
  name: string
  config: Record<string, string>
}

const ACTION_OPTIONS: { value: ActionType; label: string }[] = [
  { value: 'SendEmail', label: 'Envoyer un email' },
  { value: 'CreateFollowUp', label: 'Creer un suivi' },
  { value: 'ChangeStatus', label: 'Changer le statut' },
  { value: 'AddTag', label: 'Ajouter un tag' },
  { value: 'RemoveTag', label: 'Retirer un tag' },
  { value: 'CreateNote', label: 'Creer une note' },
  { value: 'Wait', label: 'Attendre' },
  { value: 'HttpRequest', label: 'Requete HTTP' },
  { value: 'SetVariable', label: 'Definir une variable' },
  { value: 'ExecuteWorkflow', label: 'Executer un workflow' },
]

export function AutomationBuilderPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isNew = id === 'new'
  const { data: workflow } = useWorkflow(isNew ? undefined : id)
  const createWorkflow = useCreateWorkflow()
  const updateWorkflow = useUpdateWorkflow()
  const executeWorkflow = useExecuteWorkflow()

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [triggerType, setTriggerType] = useState('Manual')
  const [steps, setSteps] = useState<StepConfig[]>([])
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (workflow) {
      setName(workflow.name)
      setDescription(workflow.description ?? '')
      if (workflow.triggerJson) {
        try {
          const trigger = JSON.parse(workflow.triggerJson)
          setTriggerType(trigger.type ?? 'Manual')
        } catch { /* ignore */ }
      }
      if (workflow.actionsJson) {
        try {
          const actions = JSON.parse(workflow.actionsJson) as StepConfig[]
          setSteps(actions.map(a => ({
            type: (a.type ?? 'CreateNote') as ActionType,
            name: a.name ?? '',
            config: a.config ?? {},
          })))
        } catch { /* ignore */ }
      }
    }
  }, [workflow])

  const addStep = () => {
    setSteps([...steps, { type: 'CreateNote', name: `Etape ${steps.length + 1}`, config: {} }])
  }

  const removeStep = (index: number) => {
    setSteps(steps.filter((_, i) => i !== index))
  }

  const updateStep = (index: number, field: keyof StepConfig, value: string) => {
    const updated = [...steps]
    if (field === 'type') {
      updated[index] = { ...updated[index], type: value as ActionType }
    } else if (field === 'name') {
      updated[index] = { ...updated[index], name: value }
    }
    setSteps(updated)
  }

  const updateStepConfig = (index: number, key: string, value: string) => {
    const updated = [...steps]
    updated[index] = { ...updated[index], config: { ...updated[index].config, [key]: value } }
    setSteps(updated)
  }

  const handleSave = async () => {
    const triggerJson = JSON.stringify({ type: triggerType })
    const actionsJson = JSON.stringify(steps)

    if (isNew) {
      const created = await createWorkflow.mutateAsync({
        name,
        description: description || null,
        triggerJson,
        actionsJson,
      })
      navigate(`/automation/builder/${created.id}`, { replace: true })
    } else if (id) {
      await updateWorkflow.mutateAsync({
        id,
        dto: {
          name,
          description: description || null,
          isEnabled: workflow?.isEnabled ?? false,
          triggerJson,
          actionsJson,
        },
      })
    }
    setSaved(true)
    setTimeout(() => setSaved(false), 2000)
  }

  const handleExecute = () => {
    if (id && !isNew) executeWorkflow.mutate(id)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="sm" onClick={() => navigate('/automation')}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <h1 className="text-2xl font-bold text-foreground">
            {isNew ? 'Nouveau workflow' : 'Editeur de workflow'}
          </h1>
        </div>
        <div className="flex gap-2">
          {!isNew && (
            <Button variant="outline" onClick={handleExecute} disabled={executeWorkflow.isPending}>
              <Play className="mr-2 h-4 w-4" /> Executer
            </Button>
          )}
          <Button onClick={handleSave} disabled={createWorkflow.isPending || updateWorkflow.isPending}>
            <Save className="mr-2 h-4 w-4" />
            {saved ? 'Sauvegarde !' : 'Sauvegarder'}
          </Button>
        </div>
      </div>

      {/* Workflow info */}
      <Card>
        <CardHeader>
          <CardTitle>Informations</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label>Nom</Label>
              <Input value={name} onChange={e => setName(e.target.value)} placeholder="Nom du workflow" />
            </div>
            <div>
              <Label>Description</Label>
              <Input value={description} onChange={e => setDescription(e.target.value)} placeholder="Description" />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Trigger */}
      <Card>
        <CardHeader>
          <CardTitle>Declencheur</CardTitle>
        </CardHeader>
        <CardContent>
          <select
            value={triggerType}
            onChange={e => setTriggerType(e.target.value)}
            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
          >
            <option value="Manual">Manuel</option>
            <option value="LeadCreated">Lead cree</option>
            <option value="LeadUpdated">Lead modifie</option>
            <option value="StatusChanged">Statut change</option>
            <option value="FollowUpDue">Relance due</option>
            <option value="EmailOpened">Email ouvert</option>
            <option value="EmailClicked">Email clique</option>
            <option value="EmailReplied">Email repondu</option>
            <option value="Cron">Planifie (Cron)</option>
          </select>
        </CardContent>
      </Card>

      {/* Actions (stepper) */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Actions</CardTitle>
          <Button variant="outline" size="sm" onClick={addStep}>
            <Plus className="mr-2 h-4 w-4" /> Ajouter une action
          </Button>
        </CardHeader>
        <CardContent>
          {steps.length === 0 ? (
            <p className="text-center py-8 text-muted-foreground">
              Aucune action. Cliquez sur "Ajouter une action" pour commencer.
            </p>
          ) : (
            <div className="space-y-3">
              {steps.map((step, i) => (
                <div key={i} className="flex items-start gap-3 rounded-lg border p-4">
                  <div className="flex items-center gap-2 pt-1">
                    <GripVertical className="h-4 w-4 text-muted-foreground" />
                    <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">
                      {i + 1}
                    </span>
                  </div>
                  <div className="flex-1 space-y-3">
                    <div className="grid grid-cols-2 gap-3">
                      <div>
                        <Label>Type</Label>
                        <select
                          value={step.type}
                          onChange={e => updateStep(i, 'type', e.target.value)}
                          className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                        >
                          {ACTION_OPTIONS.map(opt => (
                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <Label>Nom</Label>
                        <Input
                          value={step.name}
                          onChange={e => updateStep(i, 'name', e.target.value)}
                          placeholder="Nom de l'action"
                        />
                      </div>
                    </div>
                    {/* Config fields based on type */}
                    {(step.type === 'SendEmail') && (
                      <div className="grid grid-cols-2 gap-3">
                        <div>
                          <Label>Sujet</Label>
                          <Input
                            value={step.config.subject ?? ''}
                            onChange={e => updateStepConfig(i, 'subject', e.target.value)}
                          />
                        </div>
                        <div>
                          <Label>Destinataire</Label>
                          <Input
                            value={step.config.to ?? ''}
                            onChange={e => updateStepConfig(i, 'to', e.target.value)}
                          />
                        </div>
                      </div>
                    )}
                    {step.type === 'Wait' && (
                      <div>
                        <Label>Duree (secondes)</Label>
                        <Input
                          type="number"
                          value={step.config.seconds ?? '60'}
                          onChange={e => updateStepConfig(i, 'seconds', e.target.value)}
                        />
                      </div>
                    )}
                    {step.type === 'ChangeStatus' && (
                      <div>
                        <Label>Nouveau statut</Label>
                        <Input
                          value={step.config.status ?? ''}
                          onChange={e => updateStepConfig(i, 'status', e.target.value)}
                          placeholder="Qualified, Client, etc."
                        />
                      </div>
                    )}
                    {(step.type === 'AddTag' || step.type === 'RemoveTag') && (
                      <div>
                        <Label>Tag</Label>
                        <Input
                          value={step.config.tag ?? ''}
                          onChange={e => updateStepConfig(i, 'tag', e.target.value)}
                        />
                      </div>
                    )}
                    {step.type === 'CreateNote' && (
                      <div>
                        <Label>Contenu</Label>
                        <Input
                          value={step.config.content ?? ''}
                          onChange={e => updateStepConfig(i, 'content', e.target.value)}
                        />
                      </div>
                    )}
                  </div>
                  <Button variant="ghost" size="sm" onClick={() => removeStep(i)}>
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Status indicators */}
      {executeWorkflow.isSuccess && (
        <div className="flex items-center gap-2 text-green-600">
          <CheckCircle className="h-4 w-4" /> Workflow execute avec succes
        </div>
      )}
      {executeWorkflow.isError && (
        <div className="flex items-center gap-2 text-red-600">
          <XCircle className="h-4 w-4" /> Erreur lors de l'execution
        </div>
      )}
    </div>
  )
}
