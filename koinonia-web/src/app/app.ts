import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { itensDoMenu } from './menu';
import { limparSessao, rotuloDoPapel } from './sessao';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly router = inject(Router);
  protected readonly papel = signal(sessionStorage.getItem('koinonia.papel') ?? '');
  protected readonly funcionarioId = signal(sessionStorage.getItem('koinonia.funcionarioId') ?? '');
  protected readonly naEntrada = signal(caminhoDe(this.router.url) === '/entrar');

  constructor() {
    this.router.events.subscribe((evento) => {
      if (evento instanceof NavigationEnd) {
        this.naEntrada.set(caminhoDe(evento.urlAfterRedirects) === '/entrar');
        this.papel.set(sessionStorage.getItem('koinonia.papel') ?? '');
        this.funcionarioId.set(sessionStorage.getItem('koinonia.funcionarioId') ?? '');
      }
    });
  }

  protected itens() {
    return itensDoMenu(this.papel());
  }

  protected rotuloPapel() {
    return rotuloDoPapel(this.papel());
  }

  sair() {
    limparSessao();
    this.papel.set('');
    this.funcionarioId.set('');
    void this.router.navigateByUrl('/entrar');
  }
}

function caminhoDe(url: string): string {
  return url.split(/[?#]/)[0];
}
