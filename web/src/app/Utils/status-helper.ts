import {EStatus} from "../Enums/status-order.enum";


export function getStatusLabel(status: number): string {
  switch (status) {
    case EStatus.PRIJATA:
      return 'Prijatá';
    case EStatus.ZAPLATENA:
      return 'Zaplatená';
    case EStatus.VO_VYROBE:
      return 'Vo výrobe';
    case EStatus.PRIPRAVENA:
      return 'Pripravená';
    case EStatus.POSLANA:
      return 'Poslaná';
    case EStatus.ZRUSENA:
      return 'Zrušená';
    default:
      return 'Neznámy status';
  }
}
