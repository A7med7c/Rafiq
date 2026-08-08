// const apiUrl = 'https://demise-valuables-turret.ngrok-free.dev/api';
//const apiUrl = 'https://rafiqapi.runasp.net/api';
 const apiUrl = 'https://localhost:7082/api';

export const environment = {

    apiUrl,

    fileBaseUrl: apiUrl.replace(/\/api\/?$/, ''),

    googleClientId: '379411509806-qgfo9s8qiuuq058hu668snbupnfdiskq.apps.googleusercontent.com'
}