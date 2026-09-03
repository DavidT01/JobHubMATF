import { bootstrapApplication } from '@angular/platform-browser';
import { ApplicationsService } from './app/core/services/applications/applications-service';
import { ApplicationsPreview } from './preview/applications-preview';
import { PreviewApplicationsService } from './preview/preview-applications-service';

// This entry point is selected only by the explicit preview build configuration.
bootstrapApplication(ApplicationsPreview, {
  providers: [{ provide: ApplicationsService, useClass: PreviewApplicationsService }],
}).catch(error => console.error(error));
