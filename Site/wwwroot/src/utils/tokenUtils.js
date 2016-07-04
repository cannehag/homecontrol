export function tokenIsExpired() {
  let jwt = localStorage.getItem('token')
  if(jwt) {
    let jwtExp = jwt_decode(jwt).exp;
    let expiryDate = new Date(0);
    expiryDate.setUTCSeconds(jwtExp);
    
    if(new Date() < expiryDate) {
      return false;
    }
  }

  return true;
}