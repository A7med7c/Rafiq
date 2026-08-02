const apiUrl = 'https://localhost:7082/api';
// const apiUrl = 'https://demise-valuables-turret.ngrok-free.dev/api';

export const environment = {

    apiUrl,

    fileBaseUrl: apiUrl.replace(/\/api\/?$/, ''),

    googleClientId: '379411509806-qgfo9s8qiuuq058hu668snbupnfdiskq.apps.googleusercontent.com'
}