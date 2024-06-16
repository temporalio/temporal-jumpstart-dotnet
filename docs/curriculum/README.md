# Curriculum

## Setup

Let's assume you are using `localhost` for your local browser or other services.

Temporal Cloud should be setup with a Namespace on your account:
- Name: `temporal-jumpstart-dotnet`
- Region: whatever you want
- Certs: your choice or you can use the approach [below](#https-everything).

### HTTPS everything

1. Install [mkcert](https://github.com/FiloSottile/mkcert).
1. `mkcert -install` (this just installs the CA to your system)
1. `mkcert localhost` (this makes all our `localhost` servers ok for HTTPS)
    1. Note that it creates `localhost.pem` and `localhost-key.pem` files in our root dir
    2. We will use these from our different servers to serve over https where needed

