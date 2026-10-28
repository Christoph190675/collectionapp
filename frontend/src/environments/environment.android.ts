// Used by `ng build --configuration android`: the Android emulator reaches the host machine via 10.0.2.2.
// For a physical device, replace with the LAN address of the development machine.
export const environment = {
  production: false,
  apiUrl: 'http://10.0.2.2:8080/',
};
