import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AlbumDetailComponent } from './album-detail/album-detail.component';
import { GalleryHomeComponent } from './gallery-home/gallery-home.component';

const routes: Routes = [
  { path: '', component: GalleryHomeComponent },
  { path: 'folder/:folderId', component: GalleryHomeComponent },
  { path: 'album/:albumId', component: AlbumDetailComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class GalleryRoutingModule {}
