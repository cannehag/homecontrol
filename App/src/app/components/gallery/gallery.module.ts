import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AlbumDetailComponent } from './album-detail/album-detail.component';
import { GalleryHomeComponent } from './gallery-home/gallery-home.component';
import { GalleryRoutingModule } from './gallery-routing.module';

@NgModule({
  declarations: [GalleryHomeComponent, AlbumDetailComponent],
  imports: [CommonModule, GalleryRoutingModule, SharedModule],
})
export class GalleryModule {}
