import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface GalleryTreeNode {
  id: string;
  type: 'folder' | 'album';
  name: string;
  coverUrl: string | null;
  imageCount: number;
  children: GalleryTreeNode[];
}

export interface PhotoMetadata {
  fileName: string | null;
  captureDate: string | null;
  cameraMake: string | null;
  cameraModel: string | null;
  lens: string | null;
  aperture: string | null;
  shutterSpeed: string | null;
  iso: number | null;
  focalLength: string | null;
  flashFired: boolean | null;
  latitude: number | null;
  longitude: number | null;
  city: string | null;
  state: string | null;
  country: string | null;
  title: string | null;
  description: string | null;
  rights: string | null;
  creators: string[];
  keywords: string[];
}

export interface AlbumImage {
  id: string;
  thumbnailUrl: string;
  fullUrl: string;
  width: number | null;
  height: number | null;
  metadata: PhotoMetadata;
}

export interface AlbumDetail {
  albumId: string;
  albumName: string;
  images: AlbumImage[];
}

export interface GalleryStatus {
  connected: boolean;
  lightroomAuthExpiresInDays: number;
}

@Injectable({ providedIn: 'root' })
export class GalleryService {
  constructor(private http: HttpClient) {}

  getTree(): Observable<GalleryTreeNode[]> {
    return this.http.get<GalleryTreeNode[]>('/api/gallery/tree');
  }

  getAlbum(albumId: string): Observable<AlbumDetail> {
    return this.http.get<AlbumDetail>(`/api/gallery/albums/${albumId}`);
  }

  getStatus(): Observable<GalleryStatus> {
    return this.http.get<GalleryStatus>('/api/gallery/status');
  }
}
