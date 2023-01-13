import React from 'react';

const year = new Date().getFullYear();

export default (
    <div className="content has-text-centered is-size-5">
        <p>
            Copyright © {year} - Built with <a className="is-underlined" href="https://mangelmaxime.github.io/Nacara/" style={{color: "white"}}>Nacara</a>
        </p>
    </div>
)
