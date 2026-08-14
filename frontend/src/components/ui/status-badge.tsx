import { Badge } from './badge'
import type { LeadPriority, LeadStatus } from '@/types/lead'
import type { BadgeProps } from './badge'

const statusVariants: Record<LeadStatus, BadgeProps['variant']> = {
  New: 'info',
  Qualified: 'purple',
  ReadyToContact: 'warning',
  EmailPrepared: 'warning',
  EmailSent: 'success',
  FollowUp1: 'warning',
  FollowUp2: 'warning',
  Meeting: 'purple',
  ProposalSent: 'info',
  Client: 'success',
  Rejected: 'destructive',
  DoNotContact: 'destructive',
  SmsSent: 'success',
}

const priorityVariants: Record<LeadPriority, BadgeProps['variant']> = {
  Low: 'secondary',
  Medium: 'info',
  High: 'warning',
  Urgent: 'destructive',
}

interface StatusBadgeProps {
  status: LeadStatus
  label: string
}

interface PriorityBadgeProps {
  priority: LeadPriority
  label: string
}

export function StatusBadge({ status, label }: StatusBadgeProps) {
  return <Badge variant={statusVariants[status]}>{label}</Badge>
}

export function PriorityBadge({ priority, label }: PriorityBadgeProps) {
  return <Badge variant={priorityVariants[priority]}>{label}</Badge>
}
