import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ApplicationStatus } from '../../core/models/application-list-item-dto';

const LABELS: Record<ApplicationStatus, string> = {
  Submitted: 'Submitted',
  InReview: 'In review',
  Interview: 'Interview',
  Rejected: 'Rejected',
  Accepted: 'Accepted',
};

export function applicationStatusLabel(status: ApplicationStatus): string {
  return LABELS[status] ?? 'Unknown status';
}

/**
 * The single place an application status is rendered.
 *
 * Only an outcome carries colour: accepted is positive, rejected is an error. Everything in
 * between is a step in the process and stays neutral, so a list of applications does not read
 * as a wall of coloured badges.
 */
@Component({
  selector: 'app-status-chip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="status-chip" [class]="'status-chip status-chip--' + tone()">{{ label() }}</span>`,
  styles: `
    .status-chip {
      display: inline-flex;
      align-items: center;
      padding: 4px 12px;
      border-radius: 8px;
      font: var(--mat-sys-label-large);
      white-space: nowrap;
      background-color: var(--mat-sys-surface-container-highest);
      color: var(--mat-sys-on-surface-variant);
    }

    .status-chip--positive {
      background-color: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
    }

    .status-chip--negative {
      background-color: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }
  `,
})
export class StatusChipComponent {
  readonly status = input.required<ApplicationStatus>();

  protected readonly label = computed(() => applicationStatusLabel(this.status()));

  protected readonly tone = computed(() => {
    switch (this.status()) {
      case 'Accepted':
        return 'positive';
      case 'Rejected':
        return 'negative';
      default:
        return 'neutral';
    }
  });
}
