import { Injectable , inject } from "@angular/core";
import { HttpClient , HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { JobType , Job , ExperienceLevel , WorkMode } from "../models/job.model";
import { environment } from "../../environments/environment";

@Injectable({ providedIn: 'root'})
export class JobService {
    private http = inject(HttpClient);
    private baseUrl = environment.apiUrl;

    getAll(): Observable<Job[]> {
        return this.http.get<Job[]>(this.baseUrl);
    }

    getById(id: string): Observable<Job> {
        return this.http.get<Job>(`${this.baseUrl}/${id}`);
    }

    create(job: Partial<Job>): Observable<Job> {
        return this.http.post<Job>(this.baseUrl, job);
    }

    update(id: string, job: Job): Observable<Job> {
        return this.http.put<Job>(`${this.baseUrl}/${id}`, job);
    } 

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    search(query: string): Observable<Job[]> {
        const params = new HttpParams().set('query', query);
        return this.http.get<Job[]>(`${this.baseUrl}/search`, { params });
    }

    filter(
        jobType?: JobType,
        experienceLevel?: ExperienceLevel,
        workMode?: WorkMode,
        city?: string
    ): Observable<Job[]> {
        let params = new HttpParams();
        if (jobType) 
            params = params.set('jobType', jobType);
        if (experienceLevel)
            params = params.set('experienceLevel', experienceLevel);
        if (workMode)
            params = params.set('workMode', workMode);
        if (city)
            params = params.set('city', city);

        return this.http.get<Job[]>(`${this.baseUrl}/filter`, { params });
    }

    getActive(): Observable<Job[]> {
        return this.http.get<Job[]>(`${this.baseUrl}/active`);
    }

    filterBySalary(minSalary?: number, maxSalary?: number): Observable<Job[]> {
        let params = new HttpParams();
        
        if (minSalary != null)
            params = params.set('minSalary', minSalary);
        
        if (maxSalary != null)
            params = params.set('maxSalary', maxSalary);
        
        return this.http.get<Job[]>(`${this.baseUrl}/filter/salary`, { params });
    }

    getByCompanyId(companyId: string): Observable<Job[]> {
        return this.http.get<Job[]>(`${this.baseUrl}/company/${companyId}`);
    }

    getSortedBySalary(ascending: boolean = true): Observable<Job[]> {
        const params = new HttpParams().set('ascending', ascending);
        return this.http.get<Job[]>(`${this.baseUrl}/sorted/salary`, { params });
    }
}

